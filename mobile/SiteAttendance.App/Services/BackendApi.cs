using System.Net.Http.Json;
using SiteAttendance.App.Core;
using Microsoft.Extensions.Logging;

namespace SiteAttendance.App.Services;

public class BackendApi
{
    private readonly HttpClient _http;
    private readonly ILogger<BackendApi> _logger;

    // Azure backend URL - deployed and working!
    private const string BaseUrl = "https://siteattendance-api-1411956859.azurewebsites.net";

    public BackendApi(ILogger<BackendApi> logger)
    {
        _logger = logger;
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    public async Task<MobileBootstrapResponse?> GetConfigAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<MobileBootstrapResponse>(
                $"/config/mobile?userId={userId}", ct);
            _logger.LogInformation("Fetched config for user {UserId}: {SiteCount} sites", 
                userId, response?.Sites.Count ?? 0);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch config from {BaseUrl}", BaseUrl);
            throw;
        }
    }

    public async Task PostGeofenceEventAsync(GeofenceEventRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/events/geofence", request, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Posted geofence {EventType} event for site {SiteId}", 
                request.EventType, request.SiteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post geofence event");
            // TODO: Queue for retry in SQLite
            throw;
        }
    }
}
