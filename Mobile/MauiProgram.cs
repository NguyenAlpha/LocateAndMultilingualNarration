using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using SkiaSharp.Views.Maui.Controls.Hosting;
using ZXing.Net.Maui.Controls;
using Mobile.Services;
using Mobile.ViewModels;

namespace Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddHttpClient();

        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IStallService, StallService>();
        builder.Services.AddSingleton<ILanguageService, LanguageService>();
        builder.Services.AddSingleton<IAudioManager>(_ => AudioManager.Current);
        builder.Services.AddSingleton<IAudioGuideService, AudioGuideService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<LanguageViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}