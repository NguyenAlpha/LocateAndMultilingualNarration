using Microsoft.Extensions.DependencyInjection;
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
        var deviceService = Handler?.MauiContext?.Services.GetRequiredService<IDeviceService>();
        deviceService.ResetDeviceId();
        var newId = deviceService.GetOrCreateDeviceId();
        await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
            "Device ID Reset", $"New ID:\n{newId}", "OK");
    }
#endif
}