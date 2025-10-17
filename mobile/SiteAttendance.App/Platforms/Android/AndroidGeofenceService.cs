using Android.App;
using Android.Content;
using Android.Gms.Location;
using Android.Gms.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SiteAttendance.App.Core;

namespace SiteAttendance.App.Platforms.Android;

public class AndroidGeofenceService : IGeofenceService
{
    private readonly ILogger<AndroidGeofenceService> _logger;
    private readonly GeofencingClient _client;
    private PendingIntent? _pendingIntent;

    public AndroidGeofenceService(ILogger<AndroidGeofenceService> _logger)
    {
        this._logger = _logger;
        _client = LocationServices.GetGeofencingClient(Platform.AppContext);
    }

    public async Task<bool> RequestPermissionsAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                _logger.LogWarning("Location permission denied");
                return false;
            }

            // Android 10+ background location
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Q)
            {
                var bgStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
                if (bgStatus != PermissionStatus.Granted)
                {
                    _logger.LogWarning("Background location permission denied");
                    return false;
                }
            }

            _logger.LogInformation("Location permissions granted");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request permissions");
            return false;
        }
    }

    public async Task RegisterGeofencesAsync(List<Site> sites)
    {
        try
        {
            var geofences = new List<IGeofence>();

            foreach (var site in sites)
            {
                var geofence = new GeofenceBuilder()
                    .SetRequestId(site.Id)
                    .SetCircularRegion(site.Latitude, site.Longitude, site.RadiusMeters)
                    .SetExpirationDuration(Geofence.NeverExpire)
                    .SetTransitionTypes(Geofence.GeofenceTransitionEnter | Geofence.GeofenceTransitionExit)
                    .Build();

                geofences.Add(geofence);
            }

            var request = new GeofencingRequest.Builder()
                .SetInitialTrigger(GeofencingRequest.InitialTriggerEnter)
                .AddGeofences(geofences)
                .Build();

            _pendingIntent = GetGeofencePendingIntent();

            await _client.AddGeofencesAsync(request, _pendingIntent);

            _logger.LogInformation("Registered {Count} geofences", sites.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register geofences");
            throw;
        }
    }

    public async Task UnregisterAllGeofencesAsync()
    {
        try
        {
            if (_pendingIntent != null)
            {
                await _client.RemoveGeofencesAsync(_pendingIntent);
                _logger.LogInformation("Unregistered all geofences");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister geofences");
            throw;
        }
    }

    private PendingIntent GetGeofencePendingIntent()
    {
        if (_pendingIntent != null)
        {
            return _pendingIntent;
        }

        var intent = new Intent(Platform.AppContext, typeof(GeofenceBroadcastReceiver));
        _pendingIntent = PendingIntent.GetBroadcast(
            Platform.AppContext,
            0,
            intent,
            PendingIntentFlags.Mutable | PendingIntentFlags.UpdateCurrent
        );

        return _pendingIntent;
    }
}

// ============================================================================
// BROADCAST RECEIVER (handles geofence transitions in background)
// ============================================================================
[BroadcastReceiver(Enabled = true, Exported = true)]
public class GeofenceBroadcastReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent == null || context == null)
        {
            return;
        }

        var geofencingEvent = GeofencingEvent.FromIntent(intent);
        if (geofencingEvent == null || geofencingEvent.HasError)
        {
            System.Diagnostics.Debug.WriteLine($"Geofence error: {geofencingEvent?.ErrorCode}");
            return;
        }

        var transition = geofencingEvent.GeofenceTransition;
        var triggeringGeofences = geofencingEvent.TriggeringGeofences;

        if (triggeringGeofences == null || triggeringGeofences.Count == 0)
        {
            return;
        }

        var eventType = transition switch
        {
            Geofence.GeofenceTransitionEnter => "Enter",
            Geofence.GeofenceTransitionExit => "Exit",
            _ => null
        };

        if (eventType == null)
        {
            return;
        }

        foreach (var geofence in triggeringGeofences)
        {
            var siteId = geofence.RequestId;
            System.Diagnostics.Debug.WriteLine($"Geofence {eventType}: {siteId}");

            // TODO: Post event to backend via background service
            // For MVP, just log. In production, use WorkManager or foreground service
        }
    }
}
