using Microsoft.Extensions.Logging;

namespace SiteAttendance.App.Services;

public class LocalNotificationService
{
    private readonly ILogger<LocalNotificationService> _logger;

    public LocalNotificationService(ILogger<LocalNotificationService> logger)
    {
        _logger = logger;
    }

    public void ShowNotification(string title, string message)
    {
        // TODO: Implement local notifications (e.g., Plugin.LocalNotification)
        _logger.LogInformation("[LOCAL NOTIFICATION] {Title}: {Message}", title, message);
    }
}
