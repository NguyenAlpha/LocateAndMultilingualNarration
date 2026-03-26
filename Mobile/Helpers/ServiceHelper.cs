using Microsoft.Extensions.DependencyInjection;

namespace Mobile.Helpers;

public static class ServiceHelper
{
    public static T GetService<T>() where T : notnull
    {
        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
        {
            throw new InvalidOperationException("Service provider is not available.");
        }

        return services.GetRequiredService<T>();
    }
}
