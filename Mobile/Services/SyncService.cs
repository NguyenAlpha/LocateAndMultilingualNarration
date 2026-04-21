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
    /// Đồng bộ dữ liệu theo các bước: lấy preference, gọi API, lưu local và tải audio.
    /// </summary>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Task đại diện cho quá trình đồng bộ.</returns>
    public async Task SyncAsync(CancellationToken ct = default)
    {
        // Tránh chạy đồng bộ song song nhiều lần cùng lúc — atomic, không race được.
        if (Interlocked.CompareExchange(ref _syncInFlight, 1, 0) != 0) return;

        try
        {
            // Bước 1: Đọc preference từ local cache — không cần gọi API thêm.
            var deviceId = _deviceService.GetOrCreateDeviceId();
            var pref = _localPreference.Load();
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("[SyncService][SyncAsync]Language name: {LanguageName} | voice: {VoiceId}", pref?.LanguageName, pref?.VoiceId);
            var languageCode = pref?.LanguageCode ?? "vi";
            var voiceId = pref?.VoiceId?.ToString() ?? string.Empty;

            // Bước 2: Gọi API trực tiếp để lấy danh sách Stall của thiết bị hiện tại.
            var client = _httpClientFactory.CreateClient();
            var url = $"{StallsEndpoint}?deviceId={Uri.EscapeDataString(deviceId)}";
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[SyncAsync]: API trả về {StatusCode}", (int)response.StatusCode);
                return;
            }
            var stream = await response.Content.ReadAsStreamAsync(ct);
            var result = await JsonSerializer.DeserializeAsync<ApiResult<List<GeoStallDto>>>(stream, JsonOptions, ct);
            var apiStalls = result?.Data ?? [];
            if (apiStalls.Count == 0)
            {
                _logger.LogWarning("[SyncAsync]: API trả về 0 stall, bỏ qua");
                return;
            }

            // Bước 3: Load bản ghi cũ trước khi upsert để lấy AudioUrl và LocalAudioPath cũ so sánh ở bước 4.
            var existingMap = (await _localRepo.GetAllAsync()).ToDictionary(s => s.StallId);

            var localStalls = apiStalls.Select(s => new LocalStall
            {
                StallId               = s.StallId.ToString(),
                StallName             = s.StallName,
                Latitude              = s.Latitude,
                Longitude             = s.Longitude,
                RadiusMeters          = s.RadiusMeters,
                AudioUrl              = s.NarrationContent?.AudioUrl,
                LanguageCode          = languageCode,
                VoiceId               = voiceId,
                LastUpdated           = DateTimeOffset.UtcNow,
                NarrationContentId    = s.NarrationContent?.Id.ToString(),
                NarrationTitle        = s.NarrationContent?.Title,
                NarrationDescription  = s.NarrationContent?.Description,
                NarrationScriptText   = s.NarrationContent?.ScriptText
            }).ToList();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var withNarration = localStalls.Count(s => s.NarrationContentId != null);
                _logger.LogInformation("[SyncAsync]: {WithNarration}/{Total} stall có NarrationContent", withNarration, localStalls.Count);
            }

            // Ghi toàn bộ danh sách vào cơ sở dữ liệu cục bộ.
            // UpsertBatchAsync chỉ ghi những stall thực sự thay đổi — log ở đây chỉ biết tổng từ API.
            await _localRepo.UpsertBatchAsync(localStalls);
            // Xóa memory cache của StallService để lần sau đọc từ SQLite thay vì gọi API trùng lặp.
            _stallService.InvalidateCache();
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("[SyncAsync]: API trả về {Total} stall, đã upsert vào SQLite (chỉ ghi stall thay đổi)", localStalls.Count);

            // Cleanup 1: Đổi ngôn ngữ → xóa folder audio cũ để tránh tích lũy file thừa.
            var oldLanguageCode = existingMap.Values
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.LanguageCode))?.LanguageCode;
            if (oldLanguageCode != null && oldLanguageCode != languageCode)
            {
                await _audioCacheService.DeleteByLanguageAsync(oldLanguageCode);
                _logger.LogInformation("[SyncAsync]: ngôn ngữ đổi {Old}→{New}, đã xóa audio cache cũ", oldLanguageCode, languageCode);
            }

            // Cleanup 2: Stall bị xóa hoặc mất audio → xóa file local tương ứng.
            var newStallMap = localStalls.ToDictionary(s => s.StallId);
            foreach (var (stallId, old) in existingMap)
            {
                if (string.IsNullOrWhiteSpace(old.LocalAudioPath)) continue;
                var isRemovedOrLostAudio = !newStallMap.TryGetValue(stallId, out var newStall)
                    || string.IsNullOrWhiteSpace(newStall.AudioUrl);
                if (isRemovedOrLostAudio && File.Exists(old.LocalAudioPath))
                    File.Delete(old.LocalAudioPath);
            }

            // Bước 4: Tải file âm thanh song song nhưng giới hạn số luồng để tránh quá tải.
            var semaphore   = new SemaphoreSlim(3);
            var audioTotal  = localStalls.Count(s => !string.IsNullOrWhiteSpace(s.AudioUrl));
            var audioSkipped = 0;
            var audioDownloaded = 0;

            var downloadTasks = localStalls
                .Where(s => !string.IsNullOrWhiteSpace(s.AudioUrl))
                .Select(async s =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        // Bỏ qua nếu URL không đổi và file vẫn còn trên máy — không cần tải lại.
                        var old = existingMap.GetValueOrDefault(s.StallId);
                        if (old is not null
                            && old.AudioUrl == s.AudioUrl
                            && old.LocalAudioPath is not null
                            && File.Exists(old.LocalAudioPath))
                        {
                            Interlocked.Increment(ref audioSkipped);
                            return;
                        }

                        // Tải audio về cache cục bộ theo StallId và ngôn ngữ.
                        var localPath = await _audioCacheService.EnsureDownloadedAsync(
                            s.AudioUrl!, s.StallId, languageCode, ct);

                        // Nếu đã tải thành công thì cập nhật lại đường dẫn local trong SQLite.
                        if (localPath is not null)
                        {
                            await _localRepo.UpdateLocalAudioPathAsync(s.StallId, localPath);
                            Interlocked.Increment(ref audioDownloaded);
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        // Lỗi một file không làm hỏng toàn bộ batch.
                        _logger.LogWarning(ex, "[SyncAsync]: lỗi tải audio stall {StallId}", s.StallId);
                    }
                    finally { semaphore.Release(); }
                });

            // Chờ tất cả tác vụ download hoàn tất.
            await Task.WhenAll(downloadTasks);
            _firstSyncCompletedAt ??= DateTime.UtcNow;
            LastSyncedAt = DateTime.UtcNow;
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation(
                    "[SyncAsync]: audio {Downloaded} tải mới / {Skipped} bỏ qua (đã có) / {Total} tổng — hoàn tất lúc {Time}",
                    audioDownloaded, audioSkipped, audioTotal, LastSyncedAt);

            // Báo cho UI (MapViewModel) reload DTO để dùng LocalAudioPath mới.
            // InvalidateCache đã được gọi ở line 145 sau UpsertBatch — không cần gọi lại.
            if (audioDownloaded > 0)
            {
                AudioDownloaded?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException) { /* bị huỷ bình thường */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncAsync]: lỗi không mong muốn");
        }
        finally { Volatile.Write(ref _syncInFlight, 0); }
    }
}
