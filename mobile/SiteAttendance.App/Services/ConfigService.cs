using SiteAttendance.App.Core;
using Microsoft.Extensions.Logging;

namespace SiteAttendance.App.Services;

public class ConfigService
{
    private readonly BackendApi _api;
    private readonly IGeofenceService _geofenceService;
    private readonly ILogger<ConfigService> _logger;

    public RemoteConfig? Config { get; private set; }
    public List<Site> Sites { get; private set; } = new();

    public ConfigService(
        BackendApi api,
        IGeofenceService geofenceService,
        ILogger<ConfigService> logger)
    {
        _api = api;
        _geofenceService = geofenceService;
        _logger = logger;
    }

    public async Task InitializeAsync(string userId)
    {
        // Request permissions
        var granted = await _geofenceService.RequestPermissionsAsync();
        if (!granted)
        {
            throw new InvalidOperationException("Location permissions not granted");
        }

        // Try to fetch config and sites from backend
        try
        {
            var bootstrap = await _api.GetConfigAsync(userId);
            if (bootstrap != null)
            {
                Config = bootstrap.Config;
                Sites = bootstrap.Sites;
                _logger.LogInformation("Fetched {SiteCount} sites from backend", Sites.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch from backend, using mock data");
            
            // Fallback to mock data for testing
            Sites = GetMockSites();
            Config = new RemoteConfig(
                MinimumAppVersion: "1.0.0",
                GeofenceRadiusMeters: 100,
                ApiBaseUrl: "http://10.0.2.2:5001"
            );
            _logger.LogInformation("Using {SiteCount} mock sites for testing", Sites.Count);
        }

        if (Sites.Count == 0)
        {
            throw new InvalidOperationException("No sites available (backend returned empty or failed)");
        }

        // Register geofences
        await _geofenceService.RegisterGeofencesAsync(Sites);

        _logger.LogInformation("Initialized with {SiteCount} geofences", Sites.Count);
    }

    public async Task SimulateGeofenceEventAsync(
        string siteId,
        string eventType,
        double? latitude,
        double? longitude)
    {
        var request = new GeofenceEventRequest(
            UserId: "user-demo",
            SiteId: siteId,
            EventType: eventType,
            Latitude: latitude,
            Longitude: longitude
        );

        try
        {
            await _api.PostGeofenceEventAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post event to backend (offline mode)");
            // In production, would queue for retry
        }
    }

    /// <summary>
    /// Returns mock test sites for development/testing
    /// Replace these coordinates with locations near you for testing!
    /// </summary>
    private List<Site> GetMockSites()
    {
        // TODO: Replace these with actual coordinates near your location for testing
        return new List<Site>
        {
            new Site(
                Id: "site-001",
                Name: "Test Site 1 - Office",
                Latitude: 53.3498,  // Dublin, Ireland (example)
                Longitude: -6.2603,
                RadiusMeters: 100
            ),
            new Site(
                Id: "site-002",
                Name: "Test Site 2 - Warehouse",
                Latitude: 53.3520,
                Longitude: -6.2570,
                RadiusMeters: 150
            )
        };
    }
}
