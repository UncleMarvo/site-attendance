using Microsoft.Extensions.Logging;
using SiteAttendance.App.Core;
using SiteAttendance.App.Services;

namespace SiteAttendance.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register services
        builder.Services.AddSingleton<BackendApi>();
        builder.Services.AddSingleton<ConfigService>();
        builder.Services.AddSingleton<LocalNotificationService>();

        // Platform-specific geofencing
#if ANDROID
        builder.Services.AddSingleton<IGeofenceService, Platforms.Android.AndroidGeofenceService>();
#elif IOS
        builder.Services.AddSingleton<IGeofenceService, Platforms.iOS.IOSGeofenceService>();
#endif

        // Register pages
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
