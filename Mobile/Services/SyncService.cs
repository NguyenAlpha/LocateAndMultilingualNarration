using Microsoft.Extensions.Logging;
using Mobile.LocalDb;

namespace Mobile.Services;

public interface ISyncService
{
    DateTime? LastSyncedAt { get; }
    bool IsSyncing { get; }
    Task SyncAsync(CancellationToken ct = default);
}

public class SyncService : ISyncService
{
    private readonly IStallService _stallService;
    private readonly ILocalStallRepository _localRepo;
    private readonly IAudioCacheService _audioCacheService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<SyncService> _logger;

    public DateTime? LastSyncedAt { get; private set; }
    public bool IsSyncing { get; private set; }

    public SyncService(
        IStallService stallService,
        ILocalStallRepository localRepo,
        IAudioCacheService audioCacheService,
        IDevicePreferenceApiService devicePreferenceApiService,
        IDeviceService deviceService,
        ILogger<SyncService> logger)
    {
        _stallService = stallService;
        _localRepo = localRepo;
        _audioCacheService = audioCacheService;
        _devicePreferenceApiService = devicePreferenceApiService;
        _deviceService = deviceService;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken ct = default)
    {
        if (IsSyncing) return; // tránh chạy song song
        IsSyncing = true;

        try
        {
            // Bước 1: Lấy preference để biết languageCode + voiceId
            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var pref = await _devicePreferenceApiService.GetAsync(deviceId, ct);
            var languageCode = pref?.LanguageCode ?? "vi";
            var voiceId = pref?.Voice ?? string.Empty;

            // Bước 2: Gọi API lấy stall mới nhất (bỏ qua in-memory cache)
            var apiStalls = await _stallService.GetStallsAsync(forceRefresh: true, ct);
            if (apiStalls.Count == 0)
            {
                _logger.LogWarning("SyncAsync: API trả về 0 stall, bỏ qua");
                return;
            }

            // Bước 3: Map sang LocalStall và upsert SQLite
            var localStalls = apiStalls.Select(s => new LocalStall
            {
                StallId      = s.StallId.ToString(),
                StallName    = s.StallName,
                Latitude     = s.Latitude,
                Longitude    = s.Longitude,
                RadiusMeters = s.RadiusMeters,
                AudioUrl     = s.AudioUrl,
                LanguageCode = languageCode,
                VoiceId      = voiceId,
                LastUpdated  = DateTimeOffset.UtcNow
            }).ToList();

            await _localRepo.UpsertBatchAsync(localStalls);
            _logger.LogInformation("SyncAsync: upsert {Count} stall vào SQLite", localStalls.Count);

            // Bước 4: Download audio song song, tối đa 3 concurrent
            var semaphore = new SemaphoreSlim(3);
            var downloadTasks = localStalls
                .Where(s => !string.IsNullOrWhiteSpace(s.AudioUrl))
                .Select(async s =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        var localPath = await _audioCacheService.EnsureDownloadedAsync(
                            s.AudioUrl!, s.StallId, languageCode, ct);

                        if (localPath is not null)
                            await _localRepo.UpdateLocalAudioPathAsync(s.StallId, localPath);
                    }
                    finally { semaphore.Release(); }
                });

            await Task.WhenAll(downloadTasks);
            LastSyncedAt = DateTime.UtcNow;
            _logger.LogInformation("SyncAsync: hoàn tất lúc {Time}", LastSyncedAt);
        }
        catch (OperationCanceledException) { /* bị huỷ bình thường */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncAsync: lỗi không mong muốn");
        }
        finally { IsSyncing = false; }
    }
}
