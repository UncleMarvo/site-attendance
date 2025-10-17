using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Locations;
using SiteAttendance.App.Core;

namespace SiteAttendance.App.Services;

/// <summary>
/// Wrapper service that implements IGeofenceService using Shiny.NET
/// </summary>
public class ShinyGeofenceService : IGeofenceService
{
    private readonly IGeofenceManager _geofenceManager;
    private readonly ILogger<ShinyGeofenceService> _logger;

    public ShinyGeofenceService(
        IGeofenceManager geofenceManager,
        ILogger<ShinyGeofenceService> logger)
    {
        _geofenceManager = geofenceManager;
        _logger = logger;
    }

    public async Task<bool> RequestPermissionsAsync()
    {
        _logger.LogInformation("Requesting location permissions");

        var status = await _geofenceManager.RequestAccess();

        var granted = status == AccessState.Available;
        _logger.LogInformation("Location permission status: {Status} (Granted: {Granted})", 
            status, granted);

        return granted;
    }

    public async Task RegisterGeofencesAsync(List<Site> sites)
    {
        if (sites == null || sites.Count == 0)
        {
            _logger.LogWarning("No sites to register for geofencing");
            return;
        }

        _logger.LogInformation("Registering {Count} geofences", sites.Count);

        // Clear existing geofences first
        await _geofenceManager.StopAllMonitoring();

        // Register each site as a geofence
        foreach (var site in sites)
        {
            try
            {
                // Shiny GeofenceRegion constructor: (identifier, center, radius)
                var region = new GeofenceRegion(
                    site.Id, // identifier
                    new Position(site.Latitude, site.Longitude), // center
                    Distance.FromMeters(site.RadiusMeters) // radius
                )
                {
                    NotifyOnEntry = true,
                    NotifyOnExit = true,
                    SingleUse = false // Keep monitoring indefinitely
                };

                await _geofenceManager.StartMonitoring(region);
                _logger.LogInformation("Registered geofence: {SiteId} ({SiteName}) at ({Lat}, {Lon}) radius {Radius}m",
                    site.Id, site.Name, site.Latitude, site.Longitude, site.RadiusMeters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register geofence for site {SiteId}", site.Id);
            }
        }

        _logger.LogInformation("Geofence registration complete");
    }

    public async Task UnregisterAllGeofencesAsync()
    {
        _logger.LogInformation("Unregistering all geofences");
        await _geofenceManager.StopAllMonitoring();
        _logger.LogInformation("All geofences unregistered");
    }
}
