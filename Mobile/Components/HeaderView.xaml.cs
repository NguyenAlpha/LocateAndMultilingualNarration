
using Mobile.LocalDb;
using Mobile.Services;

namespace Mobile.Components;

public partial class HeaderView : Grid
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(HeaderView), string.Empty);

    public static readonly BindableProperty LogoutCommandProperty =
        BindableProperty.Create(nameof(LogoutCommand), typeof(System.Windows.Input.ICommand), typeof(HeaderView));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public System.Windows.Input.ICommand? LogoutCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(LogoutCommandProperty);
        set => SetValue(LogoutCommandProperty, value);
    }

    public HeaderView()
    {
        InitializeComponent();
        BindingContext = this;

#if DEBUG
        debugResetButton.IsVisible = true;
        debugResetButton.Clicked += OnDebugResetDeviceId;
#endif
    }

#if DEBUG
    private async void OnDebugResetDeviceId(object? sender, EventArgs e)
    {
        var confirm = await Shell.Current.CurrentPage.DisplayAlertAsync(
            "Debug Reset", "Xóa toàn bộ Preferences + SQLite và về LoadingPage?", "Xóa", "Hủy");
        if (!confirm) return;

        // Xóa SQLite
        var repo = IPlatformApplication.Current?.Services.GetService<ILocalStallRepository>();
        if (repo is not null)
            await repo.DeleteAllAsync();

        // Xóa memory cache của StallService
        var stallService = IPlatformApplication.Current?.Services.GetService<IStallService>();
        stallService?.InvalidateCache();

        // Xóa file audio đã tải
        var audioCache = IPlatformApplication.Current?.Services.GetService<IAudioCacheService>();
        if (audioCache is not null)
            await audioCache.ClearAllAsync();

        // Xóa Preferences (QR, language, device preference)
        Preferences.Clear();

        await Shell.Current.GoToAsync("//LoadingPage");
    }
#endif
}