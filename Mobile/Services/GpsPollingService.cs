using Microsoft.Extensions.Logging;

namespace Mobile.Services;

public interface IGpsPollingService
{
    /// <summary>
    /// Raised trên mỗi GPS fix thành công. Args: lat, lng, accuracy (mét).
    /// </summary>
    event Action<double, double, double?>? LocationUpdated;

    void Start();
    void Stop();
}

public class GpsPollingService : IGpsPollingService
{
    private readonly ILocationLogService _locationLogService;
    private readonly ILogger<GpsPollingService> _logger;

    private CancellationTokenSource? _cts;

    public event Action<double, double, double?>? LocationUpdated;

    public GpsPollingService(ILocationLogService locationLogService, ILogger<GpsPollingService> logger)
    {
        _locationLogService = locationLogService;
        _logger = logger;
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("[GPS] Bắt đầu polling");
        var tickCount = 0;

        while (!ct.IsCancellationRequested)
        {
            tickCount++;
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Low), ct);

                if (location is not null)
                {
                    _logger.LogDebug("[GPS] Tick #{Tick} — lat={Lat:F6}, lng={Lng:F6}", tickCount, location.Latitude, location.Longitude);
                    LocationUpdated?.Invoke(location.Latitude, location.Longitude, location.Accuracy);
                    _locationLogService.TrySample(location.Latitude, location.Longitude, location.Accuracy);
                }
                else
                {
                    _logger.LogDebug("[GPS] Tick #{Tick} — trả về null", tickCount);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[GPS] Bị hủy ở tick #{Tick}", tickCount);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[GPS] Tick #{Tick} lỗi: {Message}", tickCount, ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct).ContinueWith(_ => { });
        }

        _logger.LogInformation("[GPS] Dừng sau {Tick} tick", tickCount);
    }
}
