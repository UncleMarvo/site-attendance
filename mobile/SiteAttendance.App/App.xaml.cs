using Microsoft.Extensions.Logging;

namespace SiteAttendance.App;

public partial class App : Application
{
    public App(ILogger<App> logger)
    {
        InitializeComponent();
        MainPage = new AppShell();
        
        logger.LogInformation("SiteAttendance App started");
    }
}
