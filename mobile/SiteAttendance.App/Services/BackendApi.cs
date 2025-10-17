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
        
        // Create HttpClient with proper configuration for mobile platforms
        var handler = new HttpClientHandler
        {
            // Allow automatic decompression for better performance
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30) // 30 second timeout for mobile networks
        };
        
        // Set default headers
        _http.DefaultRequestHeaders.Add("User-Agent", "SiteAttendance-Mobile/1.0");
        
        _logger.LogInformation("BackendApi initialized with base URL: {BaseUrl}, Timeout: {Timeout}s", 
            BaseUrl, _http.Timeout.TotalSeconds);
    }

    public async Task<MobileBootstrapResponse?> GetConfigAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching config for user {UserId} from {Url}", 
                userId, $"{BaseUrl}/config/mobile?userId={userId}");
            
            // Use CancellationTokenSource to enforce our own timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            
            var response = await _http.GetFromJsonAsync<MobileBootstrapResponse>(
                $"/config/mobile?userId={userId}", cts.Token);
            
            _logger.LogInformation("Fetched config for user {UserId}: {SiteCount} sites", 
                userId, response?.Sites.Count ?? 0);
            
            return response;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request timed out after 30 seconds while fetching config from {BaseUrl}", BaseUrl);
            throw new Exception($"Connection timed out after 30 seconds. Please check your internet connection.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request timed out while fetching config from {BaseUrl}", BaseUrl);
            throw new Exception($"Connection timed out. Please check your internet connection.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while fetching config from {BaseUrl}. Message: {Message}", BaseUrl, ex.Message);
            throw new Exception($"Cannot reach server at {BaseUrl}. Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching config from {BaseUrl}", BaseUrl);
            throw;
        }
    }

    public async Task PostGeofenceEventAsync(GeofenceEventRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Posting {EventType} event for site {SiteId}", 
                request.EventType, request.SiteId);
            
            // Use CancellationTokenSource to enforce our own timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            
            var response = await _http.PostAsJsonAsync("/events/geofence", request, cts.Token);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Posted geofence {EventType} event for site {SiteId}", 
                request.EventType, request.SiteId);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request timed out after 30 seconds posting geofence event");
            throw new Exception("Connection timed out after 30 seconds.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request timed out posting geofence event");
            throw new Exception("Connection timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error posting geofence event. Message: {Message}", ex.Message);
            throw new Exception($"Cannot reach server. Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post geofence event");
            // TODO: Queue for retry in SQLite
            throw;
        }
    }
}
