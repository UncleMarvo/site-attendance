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
        
        _logger.LogInformation("🎯 ShinyGeofenceService CONSTRUCTOR called");
    }

    public async Task<bool> RequestPermissionsAsync()
    {
        _logger.LogWarning("🔐 Requesting location permissions...");

        // Request access through Shiny's geofence manager
        var status = await _geofenceManager.RequestAccess();
        
        _logger.LogWarning("🔐 Shiny permission status: {Status}", status);

        // Check if we got the required permissions
        if (status == AccessState.Available)
        {
            _logger.LogWarning("✅ All location permissions granted!");
            return true;
        }

        // Log specific permission issues
        _logger.LogError("❌ Location permissions not fully granted. Status: {Status}", status);
        
        if (status == AccessState.Restricted)
        {
            _logger.LogError("⚠️ Background location is RESTRICTED. User needs to grant 'Allow all the time' in Settings!");
        }

        // For Android 10+, background location requires separate permission
        // The user needs to select "Allow all the time" in the permission dialog
        // If they selected "Allow only while using the app", geofencing won't work
        
        return false;
    }

    public async Task RegisterGeofencesAsync(List<Site> sites)
    {
        if (sites == null || sites.Count == 0)
        {
            _logger.LogWarning("⚠️ No sites to register for geofencing");
            return;
        }

        _logger.LogWarning("📍 Registering {Count} geofences...", sites.Count);

        // Clear existing geofences first
        await _geofenceManager.StopAllMonitoring();
        _logger.LogInformation("🧹 Cleared all existing geofences");

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
                _logger.LogWarning("✅ Registered geofence: {SiteId} ({SiteName}) at ({Lat}, {Lon}) radius {Radius}m",
                    site.Id, site.Name, site.Latitude, site.Longitude, site.RadiusMeters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to register geofence for site {SiteId}", site.Id);
            }
        }

        // Verify what got registered
        var monitoredRegions = await _geofenceManager.GetMonitoredRegions();
        _logger.LogWarning("🎯 Total geofences now being monitored: {Count}", monitoredRegions.Count());
        foreach (var region in monitoredRegions)
        {
            _logger.LogInformation("   - Monitoring: {Id} at ({Lat}, {Lon})", 
                region.Identifier, region.Center.Latitude, region.Center.Longitude);
        }

        _logger.LogWarning("✅ Geofence registration complete!");
    }

    public async Task UnregisterAllGeofencesAsync()
    {
        _logger.LogInformation("🧹 Unregistering all geofences");
        await _geofenceManager.StopAllMonitoring();
        _logger.LogInformation("✅ All geofences unregistered");
    }
}
