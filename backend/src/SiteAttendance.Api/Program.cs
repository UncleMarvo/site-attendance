using SiteAttendance.Application;
using SiteAttendance.Domain;
using SiteAttendance.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpClient factory (required by Infrastructure)
builder.Services.AddHttpClient();

// Domain services
builder.Services.AddSingleton<IClock, SystemClock>();

// Repositories (in-memory for MVP)
builder.Services.AddSingleton<IGeofenceEventRepository, InMemoryGeofenceEventRepository>();
builder.Services.AddSingleton<ISiteRepository, InMemorySiteRepository>();
builder.Services.AddSingleton<IAssignmentRepository, InMemoryAssignmentRepository>();
builder.Services.AddSingleton<ISettingsRepository, InMemorySettingsRepository>();

// Providers (swappable)
builder.Services.AddSingleton<IPushProvider, MockPushProvider>();
builder.Services.AddSingleton<IEmailProvider, ConsoleEmailProvider>();

// Application handlers
builder.Services.AddScoped<LogGeofenceEventHandler>();
builder.Services.AddScoped<GetMobileBootstrap>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ============================================================================
// ENDPOINTS
// ============================================================================

app.MapGet("/", () => new
{
    Service = "SiteAttendance API",
    Version = "1.0.0-MVP",
    Endpoints = new[]
    {
        "GET /config/mobile?userId={id}",
        "POST /events/geofence",
        "GET /swagger"
    }
});

app.MapGet("/config/mobile", async (
    string userId,
    GetMobileBootstrap handler,
    CancellationToken ct) =>
{
    var response = await handler.ExecuteAsync(userId, ct);
    return Results.Ok(response);
})
.WithName("GetMobileConfig")
.WithOpenApi();

app.MapPost("/events/geofence", async (
    GeofenceEventRequest request,
    LogGeofenceEventHandler handler,
    CancellationToken ct) =>
{
    var evt = await handler.HandleAsync(
        request.UserId,
        request.SiteId,
        request.EventType,
        request.Latitude,
        request.Longitude,
        ct);
    
    return Results.Ok(evt);
})
.WithName("LogGeofenceEvent")
.WithOpenApi();

app.Run();

// ============================================================================
// DTOs
// ============================================================================

public record GeofenceEventRequest(
    string UserId,
    string SiteId,
    string EventType, // "Enter" or "Exit"
    double? Latitude,
    double? Longitude
);
