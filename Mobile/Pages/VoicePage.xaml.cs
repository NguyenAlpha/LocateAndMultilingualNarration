using Microsoft.Extensions.Logging;
using Mobile.Helpers;
using Mobile.Services;
using Shared.DTOs.DevicePreferences;
using Shared.DTOs.TtsVoiceProfiles;

namespace Mobile.Pages;

// Trang chọn giọng nói cho một ngôn ngữ cụ thể.
// Dữ liệu ngôn ngữ được truyền qua query string từ trang trước.
// Logic ở trang này khá đơn giản nên `Page` giao tiếp trực tiếp với `Service`,
// không tách thêm tầng `ViewModel` riêng để tránh làm code rườm rà.
[QueryProperty(nameof(LanguageId), "languageId")]
[QueryProperty(nameof(LanguageCode), "languageCode")]
[QueryProperty(nameof(StallId), "stallId")]
[QueryProperty(nameof(Token), "token")]
public partial class VoicePage : ContentPage
{
    private readonly IVoiceService _voiceService;
    private readonly IDeviceService _deviceService;
    private readonly IDevicePreferenceApiService _devicePreferenceApiService;
    private readonly ILogger<VoicePage> _logger;
    private bool _isNavigating;

    // Id ngôn ngữ được truyền từ trang trước
    public string LanguageId { get; set; } = string.Empty;
    // Mã ngôn ngữ được truyền từ trang trước
    public string LanguageCode { get; set; } = string.Empty;
    public string? StallId { get; set; }
    public string? Token { get; set; }

    // Inject các service cần thiết cho trang
    public VoicePage(IVoiceService voiceService, IDeviceService deviceService, IDevicePreferenceApiService devicePreferenceApiService, ILogger<VoicePage> logger)
    {
        InitializeComponent();
        _voiceService = voiceService;
        _deviceService = deviceService;
        _devicePreferenceApiService = devicePreferenceApiService;
        _logger = logger;
    }

    // Khi trang xuất hiện thì nạp lại danh sách voice
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadVoicesAsync();
    }

    // Tải danh sách voice theo LanguageId
    private async Task LoadVoicesAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            LoadingIndicator.IsVisible = false;
            await DisplayAlertAsync("Không có mạng", "Vui lòng kết nối mạng để tiếp tục.", "OK");
            await Shell.Current.GoToAsync("//MainPage");
            return;
        }

        // Nếu LanguageId không hợp lệ thì không thể tải dữ liệu
        if (!Guid.TryParse(LanguageId, out var languageGuid))
        {
            LoadingIndicator.IsVisible = false;
            EmptyLabel.IsVisible = true;
            return;
        }

        try
        {
            // Gọi service để lấy danh sách voice tương ứng với ngôn ngữ
            var voices = await _voiceService.GetVoicesByLanguageAsync(languageGuid);

            // Ẩn trạng thái loading sau khi có kết quả
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            // Nếu không có voice nào thì hiển thị thông báo trống
            if (voices.Count == 0)
            {
                EmptyLabel.IsVisible = true;
                return;
            }

            // Gán danh sách vào UI
            VoiceList.ItemsSource = voices;
            VoiceList.IsVisible = true;
        }
        catch (Exception ex)
        {
            // Khi có lỗi, tắt loading và hiển thị thông báo
            LoadingIndicator.IsVisible = false;
            await DisplayAlertAsync("Lỗi", ex.Message, "OK");
        }
    }

    // Xử lý khi người dùng chạm vào một voice trong danh sách
    async void OnVoiceTapped(object sender, TappedEventArgs e)
    {
        // Chỉ xử lý khi parameter đúng kiểu voice item
        if (e.Parameter is not TtsVoiceProfileListItemDto voice) return;
        if (_isNavigating) return;

        try
        {
            _isNavigating = true;

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("[VoicePage][OnVoiceTapped] Chọn voice: ngôn ngữ: {LanguageCode} | {VoiceId}", LanguageCode, voice.Id);

            // Lấy hoặc tạo DeviceId và thông tin thiết bị hiện tại
            var deviceId = await _deviceService.GetOrCreateDeviceIdAsync();
            var deviceInfo = _deviceService.GetDeviceInfo();

            if (!Guid.TryParse(LanguageId, out var languageGuid))
            {
                await DisplayAlertAsync("Lỗi", "LanguageId không hợp lệ.", "OK");
                return;
            }

            // Lưu device preferences ngay sau khi user chọn xong language + voice.
            await _devicePreferenceApiService.SavePreferencesAsync(new DevicePreferencesRequest
            {
                DeviceId = deviceId,
                LanguageId = languageGuid,
                Voice = voice.Id.ToString(),
                AutoPlay = true,
                Platform = deviceInfo.Platform,
                DeviceModel = deviceInfo.DeviceModel,
                Manufacturer = deviceInfo.Manufacturer,
                OsVersion = deviceInfo.OsVersion
            });

            Console.WriteLine("[DEBUG] VoicePage saved DevicePreferences thành công");
            LanguageHelper.SetLanguage(LanguageCode);
            LanguageHelper.SetVoice(voice.Id.ToString());

            var route = $"//{nameof(MapPage)}";
            if (!string.IsNullOrWhiteSpace(StallId))
                route += $"?boothId={Uri.EscapeDataString(StallId)}";

            Console.WriteLine($"[DEBUG] VoicePage navigate: {route}");
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Lỗi", $"Không thể lưu giọng đọc: {ex.Message}", "OK");
        }
        finally
        {
            _isNavigating = false;
        }
    }
}
