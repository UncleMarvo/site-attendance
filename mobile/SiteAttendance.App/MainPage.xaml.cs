using Microsoft.Extensions.Logging;
using SiteAttendance.App.Services;

namespace SiteAttendance.App;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;
    private readonly ConfigService _configService;

    public MainPage(ILogger<MainPage> logger, ConfigService configService)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
    }

    private async void OnInitializeClicked(object sender, EventArgs e)
    {
        try
        {
            StatusLabel.Text = "Initializing...";
            await _configService.InitializeAsync("user-demo");
            StatusLabel.Text = $"Initialized! Found {_configService.Sites.Count} sites";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to initialize");
        }
    }

    private async void OnSimulateEnterClicked(object sender, EventArgs e)
    {
        try
        {
            if (_configService.Sites.Count == 0)
            {
                StatusLabel.Text = "No sites available. Initialize first.";
                return;
            }

            var site = _configService.Sites[0];
            
            // Simulate a geofence enter event
            await _configService.SimulateGeofenceEventAsync(
                siteId: site.Id,
                eventType: "Enter",
                latitude: site.Latitude,
                longitude: site.Longitude
            );
            
            StatusLabel.Text = $"Simulated ENTER at: {site.Name}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to simulate enter");
        }
    }
}
