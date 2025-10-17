using Microsoft.Extensions.Logging;

namespace SiteAttendance.App;

public partial class App : Application
{
    private readonly ILogger<App> _logger;

    public App(ILogger<App> logger)
    {
        InitializeComponent();
        _logger = logger;
        _logger.LogInformation("SiteAttendance App started");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
