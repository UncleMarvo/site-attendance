using Microsoft.Extensions.Logging;
using SiteAttendance.App.Services;

namespace SiteAttendance.App;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;
    private readonly ConfigService _configService;
    private readonly BackendApi _backendApi;

    public MainPage(ILogger<MainPage> logger, ConfigService configService, BackendApi backendApi)
    {
        InitializeComponent();
        _logger = logger;
        _configService = configService;
        _backendApi = backendApi;
        
        System.Diagnostics.Debug.WriteLine("🎯🎯🎯 MainPage CONSTRUCTOR called!");
        _logger.LogWarning("🎯🎯🎯 MainPage constructor - logger working!");
    }

    private async void OnInitializeClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🚨🚨🚨 OnInitializeClicked CALLED!");
        
        try
        {
            StatusLabel.Text = "Testing connection...";
            _logger.LogWarning("🚨 Starting initialization test");
            System.Diagnostics.Debug.WriteLine("Starting initialization...");
            
            // First, test if we can reach ANY endpoint
            StatusLabel.Text = "Testing basic connectivity...";
            await TestConnectivityAsync();
            
            StatusLabel.Text = "Requesting permissions...";
            System.Diagnostics.Debug.WriteLine("Calling InitializeAsync...");
            await _configService.InitializeAsync("user-demo");
            
            StatusLabel.Text = $"✅ Monitoring {_configService.Sites.Count} sites!";
            _logger.LogWarning("✅ Initialization successful - {Count} sites", _configService.Sites.Count);
            System.Diagnostics.Debug.WriteLine($"✅ SUCCESS! Monitoring {_configService.Sites.Count} sites");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Error: {ex.Message}";
            _logger.LogError(ex, "Initialization failed");
            System.Diagnostics.Debug.WriteLine($"❌ ERROR: {ex}");
        }
    }

    private async Task TestConnectivityAsync()
    {
        try
        {
            _logger.LogInformation("Testing basic HTTP connectivity");
            System.Diagnostics.Debug.WriteLine("Testing connectivity...");
            
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            
            // Test 1: Can we reach ANY HTTPS endpoint?
            StatusLabel.Text = "Test 1: Checking internet...";
            var response1 = await client.GetStringAsync("https://www.google.com");
            _logger.LogInformation("✅ Internet works - Google reachable");
            
            // Test 2: Can we reach Azure in general?
            StatusLabel.Text = "Test 2: Checking Azure...";
            var response2 = await client.GetStringAsync("https://azure.microsoft.com");
            _logger.LogInformation("✅ Azure works - azure.microsoft.com reachable");
            
            // Test 3: Can we reach OUR Azure backend?
            StatusLabel.Text = "Test 3: Checking our backend...";
            var response3 = await client.GetStringAsync("https://siteattendance-api-1411956859.azurewebsites.net/config/mobile?userId=user-demo");
            _logger.LogInformation("✅ Backend works - received {Length} bytes", response3.Length);
            
            StatusLabel.Text = "All connectivity tests passed!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connectivity test failed");
            throw new Exception($"Connectivity test failed: {ex.Message}", ex);
        }
    }

    private async void OnSimulateEnterClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🚨🚨🚨 OnSimulateEnterClicked CALLED!");
        
        if (_configService.Sites.Count == 0)
        {
            StatusLabel.Text = "Initialize first!";
            return;
        }

        try
        {
            var firstSite = _configService.Sites[0];
            StatusLabel.Text = $"Simulating ENTER for {firstSite.Name}...";
            System.Diagnostics.Debug.WriteLine($"Simulating ENTER for {firstSite.Name}");

            await _configService.SimulateGeofenceEventAsync(
                firstSite.Id,
                "Enter",
                firstSite.Latitude,
                firstSite.Longitude
            );

            StatusLabel.Text = $"✅ ENTER event sent for {firstSite.Name}";
            System.Diagnostics.Debug.WriteLine($"✅ ENTER event sent successfully");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Error: {ex.Message}";
            _logger.LogError(ex, "Simulate enter failed");
            System.Diagnostics.Debug.WriteLine($"❌ Simulate enter error: {ex}");
        }
    }

    private async void OnSimulateExitClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🚨🚨🚨 OnSimulateExitClicked CALLED!");
        
        if (_configService.Sites.Count == 0)
        {
            StatusLabel.Text = "Initialize first!";
            return;
        }

        try
        {
            var firstSite = _configService.Sites[0];
            StatusLabel.Text = $"Simulating EXIT for {firstSite.Name}...";

            await _configService.SimulateGeofenceEventAsync(
                firstSite.Id,
                "Exit",
                firstSite.Latitude,
                firstSite.Longitude
            );

            StatusLabel.Text = $"✅ EXIT event sent for {firstSite.Name}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Error: {ex.Message}";
            _logger.LogError(ex, "Simulate exit failed");
        }
    }
}
