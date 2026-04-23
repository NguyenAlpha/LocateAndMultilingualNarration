using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mobile.LocalDb;
using Shared.DTOs.Common;
using Shared.DTOs.Geo;

namespace Mobile.Services;

/// <summary>
/// Đồng bộ dữ liệu từ API về máy, lưu vào SQLite cục bộ và tải sẵn file âm thanh.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Thời điểm đồng bộ thành công gần nhất.
    /// </summary>
    DateTime? LastSyncedAt { get; }

    /// <summary>
    /// Cho biết hiện tại service có đang đồng bộ hay không.
    /// </summary>
    bool IsSyncing { get; }

    /// <summary>
    /// Thực hiện đồng bộ dữ liệu từ API về local cache.
    /// </summary>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Task đại diện cho quá trình đồng bộ.</returns>
    Task SyncAsync(CancellationToken ct = default);

    /// <summary>
    /// Đảm bảo đã sync ít nhất một lần thành công. Nếu chưa → gọi SyncAsync; nếu rồi → no-op.
    /// MapPage gọi qua method này thay vì SyncAsync trực tiếp để rule "sync-before-map" không rò rỉ ra Page.
    /// </summary>
    /// <param name="ct">Token hủy tác vụ.</param>
    Task EnsureSyncedAsync(CancellationToken ct = default);

    /// <summary>
    /// Bắn khi có audio mới được download và ghi LocalAudioPath — ViewModel nên reload DTO.
    /// </summary>
    event EventHandler? AudioDownloaded;
}

/// <summary>
/// Triển khai đồng bộ dữ liệu Stall, cache âm thanh và cập nhật trạng thái đồng bộ.
/// </summary>
public class SyncService : ISyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILocalStallRepository _localRepo;
    private readonly IAudioCacheService _audioCacheService;
    private readonly ILocalPreferenceService _localPreference;
    private readonly IDeviceService _deviceService;
    private readonly IStallService _stallService;
    private readonly ILogger<SyncService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string StallsEndpoint = "/api/geo/stalls";

    public DateTime? LastSyncedAt { get; private set; }

    // Atomic flag — 0 = idle, 1 = đang sync. Dùng Interlocked để ConnectivityChanged + Timer không cùng enter.
    private int _syncInFlight;
    public bool IsSyncing => Volatile.Read(ref _syncInFlight) == 1;

    // Mốc sync thành công đầu tiên — dùng để EnsureSyncedAsync biết có cần gọi SyncAsync hay không.
    private DateTime? _firstSyncCompletedAt;

    public Task EnsureSyncedAsync(CancellationToken ct = default)
        => _firstSyncCompletedAt is not null ? Task.CompletedTask : SyncAsync(ct);

    public event EventHandler? AudioDownloaded;

    public SyncService(
        IHttpClientFactory httpClientFactory,
        ILocalStallRepository localRepo,
        IAudioCacheService audioCacheService,
        ILocalPreferenceService localPreference,
        IDeviceService deviceService,
        IStallService stallService,
        ILogger<SyncService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _localRepo = localRepo;
        _audioCacheService = audioCacheService;
        _localPreference = localPreference;
        _deviceService = deviceService;
        _stallService = stallService;
        _logger = logger;
    }

    /// <summary>
    /// Đồng bộ dữ liệu theo 4 bước tuần tự: đọc preference → gọi API → upsert SQLite + cleanup cache → tải audio.
    /// Atomic flag <c>_syncInFlight</c> đảm bảo chỉ 1 lần sync chạy tại một thời điểm.
    /// </summary>
    public async Task SyncAsync(CancellationToken ct = default)
    {
        // Nếu đang có sync khác chạy thì bỏ qua — atomic, không race được.
        if (Interlocked.CompareExchange(ref _syncInFlight, 1, 0) != 0) return;
        try
        {
            // Đọc preference hiện tại để biết ngôn ngữ và giọng cần sync.
            var deviceId = _deviceService.GetOrCreateDeviceId();
            var pref = _localPreference.Load();
            var languageCode = pref?.LanguageCode ?? "vi";
            var voiceId = pref?.VoiceId?.ToString() ?? string.Empty;
            _logger.LogInformation("[SyncService][SyncAsync]Language name: {LanguageName} | voice: {VoiceId}", pref?.LanguageName, pref?.VoiceId);

            // Lấy danh sách stall từ API; null = lỗi hoặc không có stall → dừng sớm.
            var apiStalls = await FetchStallsFromApiAsync(deviceId, ct);
            if (apiStalls is null) return;

            // Load bản ghi cũ TRƯỚC upsert để so sánh AudioUrl/LocalAudioPath ở các bước sau.
            var existingMap = (await _localRepo.GetAllAsync()).ToDictionary(s => s.StallId);
            var localStalls = MapToLocalStalls(apiStalls, languageCode, voiceId);

            // Ghi vào SQLite; xóa memory cache để lần sau đọc lại từ SQLite.
            await _localRepo.UpsertBatchAsync(localStalls);
            _stallService.InvalidateCache();

            // Xóa file audio cũ không còn cần (đổi ngôn ngữ hoặc stall bị xóa/mất audio).
            await CleanupAudioCacheAsync(existingMap, localStalls, languageCode);

            // Tải song song audio mới hoặc đã thay đổi URL.
            var (downloaded, skipped, total) = await DownloadAudioBatchAsync(localStalls, existingMap, languageCode, ct);

            _firstSyncCompletedAt ??= DateTime.UtcNow;
            LastSyncedAt = DateTime.UtcNow;
            _logger.LogInformation(
                "[SyncAsync]: audio {Downloaded} tải mới / {Skipped} bỏ qua (đã có) / {Total} tổng — hoàn tất lúc {Time}",
                downloaded, skipped, total, LastSyncedAt);

            // Thông báo MapViewModel reload DTO để dùng LocalAudioPath mới.
            if (downloaded > 0)
                AudioDownloaded?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) { /* bị huỷ bình thường */ }
        catch (Exception ex) { _logger.LogError(ex, "[SyncAsync]: lỗi không mong muốn"); }
        finally { Volatile.Write(ref _syncInFlight, 0); }
    }

    /// <summary>
    /// Gọi API lấy danh sách stall theo <paramref name="deviceId"/>.
    /// </summary>
    /// <returns>Danh sách stall; <c>null</c> nếu API lỗi hoặc trả về 0 stall.</returns>
    private async Task<List<GeoStallDto>?> FetchStallsFromApiAsync(string deviceId, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{StallsEndpoint}?deviceId={Uri.EscapeDataString(deviceId)}";
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[SyncAsync]: API trả về {StatusCode}", (int)response.StatusCode);
            return null;
        }
        var stream = await response.Content.ReadAsStreamAsync(ct);
        var result = await JsonSerializer.DeserializeAsync<ApiResult<List<GeoStallDto>>>(stream, JsonOptions, ct);
        List<GeoStallDto> stalls = result?.Data ?? [];
        if (stalls.Count == 0)
        {
            _logger.LogWarning("[SyncAsync]: API trả về 0 stall, bỏ qua");
            return null;
        }
        return stalls;
    }

    /// <summary>
    /// Chuyển đổi DTO từ API sang <see cref="LocalStall"/> để lưu SQLite.
    /// <paramref name="languageCode"/> và <paramref name="voiceId"/> được gắn vào mỗi bản ghi
    /// để phát hiện khi ngôn ngữ thay đổi ở lần sync tiếp theo.
    /// </summary>
    private static List<LocalStall> MapToLocalStalls(List<GeoStallDto> apiStalls, string languageCode, string voiceId)
        => apiStalls.Select(s => new LocalStall
        {
            StallId              = s.StallId.ToString(),
            StallName            = s.StallName,
            Latitude             = s.Latitude,
            Longitude            = s.Longitude,
            RadiusMeters         = s.RadiusMeters,
            AudioUrl             = s.NarrationContent?.AudioUrl,
            LanguageCode         = languageCode,
            VoiceId              = voiceId,
            LastUpdated          = DateTimeOffset.UtcNow,
            NarrationContentId   = s.NarrationContent?.Id.ToString(),
            NarrationTitle       = s.NarrationContent?.Title,
            NarrationDescription = s.NarrationContent?.Description,
            NarrationScriptText  = s.NarrationContent?.ScriptText
        }).ToList();

    /// <summary>
    /// Xóa file audio cũ không còn cần thiết sau khi sync:
    /// <list type="bullet">
    ///   <item>Đổi ngôn ngữ → xóa toàn bộ folder ngôn ngữ cũ.</item>
    ///   <item>Stall bị xóa hoặc mất audio → xóa file mp3 tương ứng.</item>
    /// </list>
    /// </summary>
    private async Task CleanupAudioCacheAsync(
        Dictionary<string, LocalStall> existingMap,
        List<LocalStall> localStalls,
        string languageCode)
    {
        // Lấy ngôn ngữ cũ từ bất kỳ bản ghi nào trong SQLite (tất cả cùng ngôn ngữ).
        var oldLanguageCode = existingMap.Values
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.LanguageCode))?.LanguageCode;
        if (oldLanguageCode != null && oldLanguageCode != languageCode)
        {
            // Xóa toàn bộ folder cũ — file mới sẽ được tải vào folder ngôn ngữ mới.
            await _audioCacheService.DeleteByLanguageAsync(oldLanguageCode);
            _logger.LogInformation("[SyncAsync]: ngôn ngữ đổi {Old}→{New}, đã xóa audio cache cũ", oldLanguageCode, languageCode);
        }

        // Xóa file của stall không còn trong danh sách API hoặc đã bị xóa audio.
        var newStallMap = localStalls.ToDictionary(s => s.StallId);
        foreach (var (stallId, old) in existingMap)
        {
            if (string.IsNullOrWhiteSpace(old.LocalAudioPath)) continue;

            var stillHasAudio = newStallMap.TryGetValue(stallId, out var newStall)
                && !string.IsNullOrWhiteSpace(newStall.AudioUrl);
            if (!stillHasAudio && File.Exists(old.LocalAudioPath))
                File.Delete(old.LocalAudioPath);
        }
    }

    /// <summary>
    /// Tải audio cho tất cả stall có <c>AudioUrl</c>, tối đa 3 luồng song song.
    /// Bỏ qua stall nếu URL không đổi và file vẫn còn trên máy.
    /// </summary>
    /// <returns>Tuple (số tải mới, số bỏ qua, tổng cần tải).</returns>
    private async Task<(int downloaded, int skipped, int total)> DownloadAudioBatchAsync(
        List<LocalStall> localStalls,
        Dictionary<string, LocalStall> existingMap,
        string languageCode,
        CancellationToken ct)
    {
        // Giới hạn 3 download song song để tránh quá tải mạng.
        var semaphore = new SemaphoreSlim(3);
        var total = localStalls.Count(s => !string.IsNullOrWhiteSpace(s.AudioUrl));
        var skipped = 0;
        var downloaded = 0;

        var tasks = localStalls
            .Where(s => !string.IsNullOrWhiteSpace(s.AudioUrl))
            .Select(async s =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    // Bỏ qua nếu URL không đổi và file vẫn còn trên máy — tiết kiệm băng thông.
                    var old = existingMap.GetValueOrDefault(s.StallId);
                    if (old is not null
                        && old.AudioUrl == s.AudioUrl
                        && old.LocalAudioPath is not null
                        && File.Exists(old.LocalAudioPath))
                    {
                        Interlocked.Increment(ref skipped);
                        return;
                    }
                    // Tải file mới; nếu thành công ghi lại đường dẫn local vào SQLite.
                    var localPath = await _audioCacheService.EnsureDownloadedAsync(s.AudioUrl!, s.StallId, languageCode, ct);
                    if (localPath is not null)
                    {
                        await _localRepo.UpdateLocalAudioPathAsync(s.StallId, localPath);
                        Interlocked.Increment(ref downloaded);
                    }
                }
                catch (OperationCanceledException) { throw; }
                // Lỗi 1 file không làm hỏng toàn bộ batch.
                catch (Exception ex) { _logger.LogWarning(ex, "[SyncAsync]: lỗi tải audio stall {StallId}", s.StallId); }
                finally { semaphore.Release(); }
            });

        await Task.WhenAll(tasks);
        return (downloaded, skipped, total);
    }
}
