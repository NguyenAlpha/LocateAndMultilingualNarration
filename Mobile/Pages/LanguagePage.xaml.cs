using Mobile.Helpers;
using Mobile.Pages;
using Mobile.Services;

namespace Mobile;

/// <summary>
/// Màn hình hiển thị danh sách ngôn ngữ để người dùng chọn trước khi sang màn hình chọn giọng đọc.
/// </summary>
public partial class LanguagePage : ContentPage
{
    private readonly ILanguageService _languageService;

    /// <summary>
    /// Khởi tạo trang và gán service lấy danh sách ngôn ngữ.
    /// </summary>
    /// <param name="languageService">Service dùng để tải dữ liệu ngôn ngữ.</param>
    public LanguagePage(ILanguageService languageService)
    {
        InitializeComponent();
        _languageService = languageService;
    }

    /// <summary>
    /// Khi trang xuất hiện thì tải danh sách ngôn ngữ từ service.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLanguagesAsync();
    }

    /// <summary>
    /// Tải danh sách ngôn ngữ và binding vào danh sách hiển thị.
    /// </summary>
    /// <returns>Task bất đồng bộ của quá trình tải dữ liệu.</returns>
    private async Task LoadLanguagesAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            LoadingIndicator.IsVisible = false;
            await DisplayAlertAsync("Không có mạng", "Vui lòng kết nối mạng để tiếp tục.", "OK");
            await Shell.Current.GoToAsync("//MainPage");
            return;
        }

        try
        {
            // Lấy danh sách ngôn ngữ từ API/cache.
            var languages = await _languageService.GetLanguagesAsync();

            // Chuyển DTO thành item dùng cho UI.
            var items = languages.Select(l => new LanguageItem
            {
                Code = l.Code,
                LanguageId = l.Id,
                FlagEmoji = FlagCodeToEmoji(l.FlagCode),
                DisplayLabel = l.DisplayName ?? l.Name
            }).ToList();

            // Gán dữ liệu và bật hiển thị danh sách.
            LanguageList.ItemsSource = items;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LanguageList.IsVisible = true;
        }
        catch (InvalidOperationException ex) when (ex.Message == "no_network")
        {
            LoadingIndicator.IsVisible = false;
            await DisplayAlertAsync("Không có mạng", "Vui lòng kết nối mạng để chọn ngôn ngữ.", "OK");
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            // Ẩn loading khi có lỗi và báo lỗi cho người dùng.
            LoadingIndicator.IsVisible = false;
            await DisplayAlertAsync("Lỗi", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Xử lý khi người dùng chọn một ngôn ngữ.
    /// </summary>
    /// <param name="sender">Nguồn sự kiện.</param>
    /// <param name="e">Thông tin tap chứa item được chọn.</param>
    async void OnLanguageTapped(object sender, TappedEventArgs e)
    {
        // Bỏ qua nếu tap không mang theo dữ liệu hợp lệ.
        if (e.Parameter is not LanguageItem item) return;

        // Điều hướng sang màn hình chọn voice với languageId và languageCode.
        await Shell.Current.GoToAsync(
            $"{nameof(VoicePage)}?languageId={item.LanguageId}&languageCode={Uri.EscapeDataString(item.Code)}");
    }

    /// <summary>
    /// Chuyển mã cờ quốc gia thành emoji lá cờ để hiển thị trên UI.
    /// </summary>
    /// <param name="flagCode">Mã quốc gia dạng ISO 2 ký tự.</param>
    /// <returns>Emoji lá cờ tương ứng; nếu không hợp lệ thì trả về biểu tượng mặc định.</returns>
    private static string FlagCodeToEmoji(string? flagCode)
    {
        // Nếu mã không hợp lệ thì dùng emoji chung cho ngôn ngữ.
        if (string.IsNullOrWhiteSpace(flagCode) || flagCode.Length < 2)
            return "🌐";

        // Tạo cờ từ 2 ký tự quốc gia theo Unicode regional indicator.
        var code = flagCode.ToUpperInvariant();
        return char.ConvertFromUtf32(0x1F1E6 + (code[0] - 'A'))
             + char.ConvertFromUtf32(0x1F1E6 + (code[1] - 'A'));
    }

    /// <summary>
    /// Model nội bộ dùng để binding dữ liệu ngôn ngữ cho danh sách hiển thị.
    /// </summary>
    private class LanguageItem
    {
        /// <summary>Mã ngôn ngữ.</summary>
        public string Code { get; set; } = null!;

        /// <summary>Mã định danh ngôn ngữ.</summary>
        public Guid LanguageId { get; set; }

        /// <summary>Emoji lá cờ đại diện.</summary>
        public string FlagEmoji { get; set; } = null!;

        /// <summary>Nhãn hiển thị trên UI.</summary>
        public string DisplayLabel { get; set; } = null!;
    }
}
