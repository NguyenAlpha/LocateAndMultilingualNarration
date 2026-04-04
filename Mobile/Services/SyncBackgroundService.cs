using Microsoft.Extensions.Logging;

namespace Mobile.Services;

/// <summary>
/// Quản lý việc đồng bộ nền định kỳ và đồng bộ lại ngay khi mạng được khôi phục.
/// </summary>
public interface ISyncBackgroundService
{
    /// <summary>
    /// Bắt đầu cơ chế đồng bộ nền.
    /// </summary>
    void Start();

    /// <summary>
    /// Dừng cơ chế đồng bộ nền và giải phóng tài nguyên.
    /// </summary>
    void Stop();
}

/// <summary>
/// Triển khai đồng bộ nền dựa trên timer định kỳ và sự kiện thay đổi kết nối mạng.
/// </summary>
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

    /// <summary>
    /// Bắt đầu đăng ký theo dõi mạng, chạy timer định kỳ và sync lần đầu nếu đang có mạng.
    /// </summary>
    public void Start()
    {
        // Đảm bảo không đăng ký trùng hoặc chạy song song nhiều instance.
        Stop(); // tránh double-start
        _cts = new CancellationTokenSource();

        // Lắng nghe thay đổi kết nối mạng
        // Khi mạng trở lại, service sẽ sync ngay mà không cần chờ timer.
        Connectivity.ConnectivityChanged += OnConnectivityChanged;

        // Chạy timer định kỳ
        // Vòng lặp nền này sẽ tự dừng khi CancellationToken bị hủy.
        _ = RunPeriodicAsync(_cts.Token);

        // Sync lần đầu ngay khi start (nếu có mạng)
        // Nếu đang online thì chủ động đồng bộ ngay để dữ liệu sớm được cập nhật.
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            _logger.LogInformation("[SyncBackgroundService][Start]: Start — sync lần đầu");
            _ = _syncService.SyncAsync(_cts.Token);
        }
    }

    /// <summary>
    /// Dừng theo dõi mạng, hủy timer và giải phóng token nguồn.
    /// </summary>
    public void Stop()
    {
        // Hủy đăng ký sự kiện để tránh leak và callback ngoài ý muốn.
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        // Hủy các tác vụ đang chạy.
        _cts?.Cancel();
        // Giải phóng token nguồn sau khi hủy.
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// Chạy đồng bộ định kỳ theo chu kỳ đã cấu hình.
    /// </summary>
    /// <param name="ct">Token hủy để dừng vòng lặp nền.</param>
    private async Task RunPeriodicAsync(CancellationToken ct)
    {
        // Timer lặp theo khoảng thời gian cố định.
        using var timer = new PeriodicTimer(SyncInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                // Nếu không có mạng thì bỏ qua lần tick này.
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    _logger.LogDebug("SyncBackgroundService: không có mạng, bỏ qua tick");
                    continue;
                }
                // Có mạng thì đồng bộ dữ liệu.
                _logger.LogInformation("SyncBackgroundService: periodic tick → sync");
                await _syncService.SyncAsync(ct);
            }
        }
        catch (OperationCanceledException) { /* normal stop */ }
    }

    /// <summary>
    /// Xử lý sự kiện thay đổi kết nối mạng và kích hoạt sync ngay khi có Internet.
    /// </summary>
    /// <param name="sender">Nguồn phát sinh sự kiện.</param>
    /// <param name="e">Thông tin thay đổi kết nối mạng.</param>
    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        // Chỉ sync khi mạng vừa được khôi phục.
        if (e.NetworkAccess != NetworkAccess.Internet) return;
        // Nếu service đã bị dừng thì không chạy nữa.
        if (_cts is null || _cts.IsCancellationRequested) return;

        // Khi có mạng trở lại, đồng bộ ngay để giảm độ trễ dữ liệu.
        _logger.LogInformation("SyncBackgroundService: mạng kết nối lại → sync ngay");
        _ = _syncService.SyncAsync(_cts.Token);
    }
}
