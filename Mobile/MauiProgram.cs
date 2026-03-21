using Microsoft.Extensions.Logging;
using ZXing.Net.Maui;
namespace Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
      .UseMauiApp<App>()
      .UseBarcodeReader() 
      .ConfigureFonts(fonts =>
      {
          fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
      });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}