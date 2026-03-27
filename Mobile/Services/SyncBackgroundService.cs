using Microsoft.Extensions.Logging;

namespace Mobile.Services;

public interface ISyncBackgroundService
{
    void Start();
    void Stop();
}

public class SyncBackgroundService : ISyncBackgroundService
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncBackgroundService> _logger;

    private CancellationTokenSource? _cts;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(3);

    public SyncBackgroundService(ISyncService syncService, ILogger<SyncBackgroundService> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    public void Start()
    {
        Stop(); // tránh double-start
        _cts = new CancellationTokenSource();

        // Lắng nghe thay đổi kết nối mạng
        Connectivity.ConnectivityChanged += OnConnectivityChanged;

        // Chạy timer định kỳ
        _ = RunPeriodicAsync(_cts.Token);

        // Sync lần đầu ngay khi start (nếu có mạng)
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            _logger.LogInformation("SyncBackgroundService: Start — sync lần đầu");
            _ = _syncService.SyncAsync(_cts.Token);
        }
    }

    public void Stop()
    {
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunPeriodicAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(SyncInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    _logger.LogDebug("SyncBackgroundService: không có mạng, bỏ qua tick");
                    continue;
                }
                _logger.LogInformation("SyncBackgroundService: periodic tick → sync");
                await _syncService.SyncAsync(ct);
            }
        }
        catch (OperationCanceledException) { /* normal stop */ }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess != NetworkAccess.Internet) return;
        if (_cts is null || _cts.IsCancellationRequested) return;

        _logger.LogInformation("SyncBackgroundService: mạng kết nối lại → sync ngay");
        _ = _syncService.SyncAsync(_cts.Token);
    }
}
