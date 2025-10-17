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

        // Fetch config and sites
        var bootstrap = await _api.GetConfigAsync(userId);
        if (bootstrap == null)
        {
            throw new InvalidOperationException("Failed to fetch config");
        }

        Config = bootstrap.Config;
        Sites = bootstrap.Sites;

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

        await _api.PostGeofenceEventAsync(request);
    }
}
