using Api.Domain.Entities;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Services
{
    /// <summary>
    /// Background service chạy liên tục trong vòng đời của API,
    /// chịu trách nhiệm xử lý các job TTS (Text-to-Speech) bất đồng bộ.
    ///
    /// Lý do tồn tại:
    ///   Azure TTS + Translation + Blob upload có thể mất 10–100 giây mỗi lần.
    ///   Nếu gọi đồng bộ trong HTTP request, Web client sẽ timeout.
    ///   Giải pháp: controller lưu content với TtsStatus="Pending" rồi trả về ngay,
    ///   service này nhận job từ DB và xử lý ở nền.
    ///
    /// Flow:
    ///   1. Mỗi 5 giây, poll DB tìm các StallNarrationContent có TtsStatus="Pending".
    ///   2. Claim batch (tối đa 5 jobs) bằng cách set TtsStatus="Processing" trước
    ///      khi bắt đầu — tránh job bị xử lý 2 lần nếu có nhiều instance API.
    ///   3. Gọi INarrationAudioService để sinh audio qua Azure TTS.
    ///   4. Thành công → TtsStatus="Completed"; thất bại → TtsStatus="Failed" + lưu lỗi.
    ///   5. Khi khởi động, reset các job bị kẹt ở "Processing" quá 10 phút
    ///      (có thể do API crash giữa chừng).
    /// </summary>
    public class TtsBackgroundService : BackgroundService
    {
        /// <summary>Khoảng thời gian giữa các lần poll DB.</summary>
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Job ở trạng thái "Processing" quá thời gian này được coi là stale (cũ)
        /// (API có thể đã crash) và sẽ được reset về "Pending" để thử lại.
        /// </summary>
        private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(10);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TtsBackgroundService> _logger;

        /// <param name="scopeFactory">
        ///   Dùng để tạo DI scope mỗi tick — bắt buộc vì BackgroundService là Singleton
        ///   nhưng AppDbContext và INarrationAudioService là Scoped.
        ///   Inject trực tiếp Scoped service vào Singleton sẽ gây lỗi runtime.
        /// </param>
        public TtsBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TtsBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Vòng lặp chính: chạy mỗi <see cref="PollInterval"/> giây cho đến khi
        /// ứng dụng tắt (stoppingToken bị cancel).
        /// PeriodicTimer tự động bỏ tick bị bỏ lỡ (không tích lũy), an toàn hơn Thread.Sleep.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(PollInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessPendingJobsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Bắt lỗi để vòng lặp không bị dừng khi gặp exception bất ngờ
                    _logger.LogError(ex, "TtsBackgroundService: lỗi không mong đợi khi xử lý jobs.");
                }
            }
        }

        /// <summary>
        /// Một tick xử lý — chạy mỗi 5 giây, làm 3 việc theo thứ tự:
        ///
        /// 1. Mở scope DI mới để lấy DbContext và NarrationAudioService dùng trong tick này.
        ///    Scope tự đóng khi hàm kết thúc → DbContext bị huỷ, không bị giữ lâu.
        ///
        /// 2. Cứu job bị kẹt: tìm các job đang "Processing" mà UpdatedAt đã quá
        ///    <see cref="StaleThreshold"/> (10 phút) — dấu hiệu API đã crash giữa chừng.
        ///    Reset chúng về "Pending" để được xử lý lại ở tick tiếp theo.
        ///
        /// 3. Nhận job mới: lấy tối đa 5 job "Pending" cũ nhất, set thành "Processing"
        ///    và lưu DB TRƯỚC khi gọi Azure TTS. Mục đích: nếu API crash sau khi TTS xong
        ///    nhưng trước khi lưu kết quả, job chỉ bị kẹt tạm thời (10 phút) thay vì
        ///    bị chạy lại và tạo audio trùng.
        /// </summary>
        private async Task ProcessPendingJobsAsync(CancellationToken ct)
        {
            // Tạo scope mới mỗi tick để lấy fresh DbContext và NarrationAudioService
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var narrationService = scope.ServiceProvider.GetRequiredService<INarrationAudioService>();

            // ── Bước 1: Reset stale Processing jobs ──────────────────────────────
            // Nếu API crash khi đang xử lý, job sẽ mắc kẹt ở "Processing" mãi.
            // Phát hiện bằng cách kiểm tra UpdatedAt: nếu đã qua StaleThreshold → reset về Pending.
            var staleCutoff = DateTimeOffset.UtcNow.Subtract(StaleThreshold);
            var staleJobs = await db.StallNarrationContents
                .Where(c => c.TtsStatus == TtsJobStatus.Processing
                         && c.UpdatedAt < staleCutoff)
                .ToListAsync(ct);

            if (staleJobs.Count > 0)
            {
                foreach (var s in staleJobs) s.TtsStatus = TtsJobStatus.Pending;
                await db.SaveChangesAsync(ct);
                _logger.LogWarning("TtsBackgroundService: reset {Count} stale Processing jobs về Pending.", staleJobs.Count);
            }

            // ── Bước 2: Claim batch Pending jobs ─────────────────────────────────
            // Lấy tối đa 5 job cũ nhất, set Processing trước khi xử lý.
            // Việc set Processing trước (optimistic claim) đảm bảo nếu có nhiều
            // instance API chạy đồng thời, mỗi job chỉ được một instance xử lý.
            var jobs = await db.StallNarrationContents
                .Where(c => c.TtsStatus == TtsJobStatus.Pending)
                .OrderBy(c => c.UpdatedAt)
                .Take(5)
                .ToListAsync(ct);

            if (jobs.Count == 0) return;

            foreach (var job in jobs) job.TtsStatus = TtsJobStatus.Processing;
            await db.SaveChangesAsync(ct); // commit claim trước khi gọi Azure

            // ── Bước 3: Xử lý từng job tuần tự ──────────────────────────────────
            // Tuần tự thay vì song song để tránh bão request đồng thời lên Azure TTS.
            foreach (var job in jobs)
                await ProcessSingleJobAsync(job, db, narrationService, ct);
        }

        /// <summary>
        /// Xử lý một job TTS: gọi Azure TTS rồi cập nhật trạng thái.
        /// </summary>
        private async Task ProcessSingleJobAsync(
            StallNarrationContent job,
            AppDbContext db,
            INarrationAudioService narrationService,
            CancellationToken ct)
        {
            _logger.LogInformation("TtsBackgroundService: bắt đầu TTS cho ContentId={Id}.", job.Id);
            try
            {
                // Gọi service hiện có — không thay đổi NarrationAudioService
                await narrationService.CreateOrUpdateFromTtsAsync(
                    job.Id, job.ScriptText, job.LanguageId, null, null);

                job.TtsStatus = TtsJobStatus.Completed;
                job.TtsError  = null;
                job.UpdatedAt = DateTimeOffset.UtcNow;
                _logger.LogInformation("TtsBackgroundService: hoàn thành TTS cho ContentId={Id}.", job.Id);
            }
            catch (Exception ex)
            {
                // Lưu lỗi vào TtsError (tối đa 500 ký tự) để hiển thị trên Web
                _logger.LogError(ex, "TtsBackgroundService: TTS thất bại cho ContentId={Id}.", job.Id);
                job.TtsStatus = TtsJobStatus.Failed;
                job.TtsError  = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                job.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
