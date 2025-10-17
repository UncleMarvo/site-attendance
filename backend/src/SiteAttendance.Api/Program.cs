using SiteAttendance.Application;
using SiteAttendance.Domain;
using SiteAttendance.Infrastructure;
using SiteAttendance.Infrastructure.Data;
using SiteAttendance.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpClient factory (required by Infrastructure)
builder.Services.AddHttpClient();

// Domain services
builder.Services.AddSingleton<IClock, SystemClock>();

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<SiteAttendanceDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    ));

// Repositories (EF Core + PostgreSQL)
builder.Services.AddScoped<IGeofenceEventRepository, EfGeofenceEventRepository>();
builder.Services.AddScoped<ISiteRepository, EfSiteRepository>();
builder.Services.AddScoped<IAssignmentRepository, EfAssignmentRepository>();
builder.Services.AddSingleton<ISettingsRepository, InMemorySettingsRepository>(); // Keep in-memory for now

// Providers (swappable)
builder.Services.AddSingleton<IPushProvider, MockPushProvider>();
builder.Services.AddSingleton<IEmailProvider, ConsoleEmailProvider>();

// Application handlers
builder.Services.AddScoped<LogGeofenceEventHandler>();
builder.Services.AddScoped<GetMobileBootstrap>();

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SiteAttendanceDbContext>();
    
    // Apply any pending migrations
    await db.Database.MigrateAsync();
    
    // Seed initial data
    await DbInitializer.SeedAsync(db);
}

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
