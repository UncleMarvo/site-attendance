using Microsoft.Extensions.Logging;
using Shiny.Locations;

namespace SiteAttendance.App.Services;

/// <summary>
/// Handles geofence enter/exit events from Shiny
/// </summary>
public class SiteAttendanceGeofenceDelegate : IGeofenceDelegate
{
    private readonly ILogger<SiteAttendanceGeofenceDelegate> _logger;
    private readonly BackendApi _api;
    private readonly LocalNotificationService _notificationService;

    public SiteAttendanceGeofenceDelegate(
        ILogger<SiteAttendanceGeofenceDelegate> logger,
        BackendApi api,
        LocalNotificationService notificationService)
    {
        _logger = logger;
        _api = api;
        _notificationService = notificationService;
    }

    public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
        var eventType = newStatus switch
        {
            GeofenceState.Entered => "Enter",
            GeofenceState.Exited => "Exit",
            _ => null
        };

        if (eventType == null)
        {
            _logger.LogWarning("Unknown geofence state: {State}", newStatus);
            return;
        }

        _logger.LogInformation("Geofence {EventType}: {SiteId} at {Timestamp}", 
            eventType, region.Identifier, DateTime.UtcNow);

        try
        {
            // Post event to backend
            var request = new Core.GeofenceEventRequest(
                UserId: "user-demo", // TODO: Get from authenticated user
                SiteId: region.Identifier,
                EventType: eventType,
                Latitude: region.Center.Latitude,
                Longitude: region.Center.Longitude
            );

            await _api.PostGeofenceEventAsync(request);

            // Show local notification (sync method)
            _notificationService.ShowNotification(
                title: $"Site {eventType}",
                message: $"You {eventType.ToLower()}ed site: {region.Identifier}"
            );

            _logger.LogInformation("Successfully posted geofence event to backend");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process geofence event for {SiteId}", region.Identifier);
            
            // Still show notification even if backend fails
            _notificationService.ShowNotification(
                title: "Geofence Event (Offline)",
                message: $"Site {eventType}: {region.Identifier} (not synced)"
            );
        }
    }
}
