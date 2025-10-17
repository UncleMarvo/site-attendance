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
            StatusLabel.Text = "Initialized!";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to initialize");
        }
    }

    private async void OnFetchSitesClicked(object sender, EventArgs e)
    {
        try
        {
            StatusLabel.Text = "Fetching sites...";
            var sites = await _configService.FetchAndStoreSitesAsync();
            StatusLabel.Text = $"Found {sites.Count} sites";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to fetch sites");
        }
    }

    private async void OnSimulateEnterClicked(object sender, EventArgs e)
    {
        try
        {
            var sites = await _configService.GetStoredSitesAsync();
            if (sites.Count == 0)
            {
                StatusLabel.Text = "No sites available. Fetch sites first.";
                return;
            }

            var site = sites[0];
            StatusLabel.Text = $"Simulated enter: {site.Name}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to simulate enter");
        }
    }
}
