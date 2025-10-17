using Microsoft.Extensions.Logging;
using SiteAttendance.App.Core;
using SiteAttendance.App.Services;
using Shiny;

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

        // Configure Shiny for background services and geofencing
        builder.UseShiny();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register services
        builder.Services.AddSingleton<BackendApi>();
        builder.Services.AddSingleton<ConfigService>();
        builder.Services.AddSingleton<LocalNotificationService>();

        // Register Shiny geofencing service wrapper
        builder.Services.AddSingleton<IGeofenceService, Services.ShinyGeofenceService>();

        // Register geofence event delegate
        builder.Services.AddGeofencing<Services.SiteAttendanceGeofenceDelegate>();

        // Register pages
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
