using PropertyInventory.Models;
using PropertyInventory.Services;
using Microsoft.AspNetCore.SignalR;
using PropertyInventory.Hubs;

namespace PropertyInventory.Services;

public class OverduePropertyNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverduePropertyNotificationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Check once per day
    private readonly TimeSpan _initialDelay = TimeSpan.FromMinutes(5); // Wait 5 minutes after startup

    public OverduePropertyNotificationService(
        IServiceProvider serviceProvider,
        ILogger<OverduePropertyNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for initial delay before first check
        await Task.Delay(_initialDelay, stoppingToken);

        _logger.LogInformation("OverduePropertyNotificationService started. Will check for overdue properties every {Interval} hours.", 
            _checkInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndNotifyOverduePropertiesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking for overdue properties");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndNotifyOverduePropertiesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for overdue properties...");

        using var scope = _serviceProvider.CreateScope();
        var firebaseService = scope.ServiceProvider.GetRequiredService<FirebaseService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<PropertyHub>>();

        try
        {
            // Get all overdue properties
            var overdueProperties = await firebaseService.GetOverduePropertiesAsync();

            if (overdueProperties == null || !overdueProperties.Any())
            {
                _logger.LogInformation("No overdue properties found.");
                return;
            }

            _logger.LogInformation("Found {Count} overdue property(ies)", overdueProperties.Count);

            // Filter properties that haven't been notified yet
            var propertiesToNotify = overdueProperties.Where(p => !p.OverdueNotificationSent).ToList();

            if (!propertiesToNotify.Any())
            {
                _logger.LogInformation("All overdue properties have already been notified. Skipping notification.");
                return;
            }

            _logger.LogInformation("Sending overdue property notification via SignalR for {Count} property(ies)", propertiesToNotify.Count);

            // Send system notification via SignalR to all connected clients
            foreach (var property in propertiesToNotify)
            {
                var daysOverdue = (DateTime.UtcNow - property.ReturnDate!.Value.ToUniversalTime()).Days;
                var returnDateStr = property.ReturnDate.Value.ToLocalTime().ToString("MMM dd, yyyy");
                
                var notificationMessage = $"{property.PropertyName} (Tag: {property.SerialNumber ?? property.PropertyCode}) borrowed by {property.BorrowerName} is {daysOverdue} day(s) overdue. Return date was {returnDateStr}.";

                // Send notification to all connected clients
                await hubContext.Clients.All.SendAsync("OverduePropertyNotification", new
                {
                    propertyId = property.Id,
                    propertyCode = property.PropertyCode,
                    propertyName = property.PropertyName,
                    tagNumber = property.SerialNumber ?? property.PropertyCode,
                    borrowerName = property.BorrowerName,
                    returnDate = returnDateStr,
                    daysOverdue = daysOverdue,
                    message = notificationMessage
                });

                _logger.LogInformation("Sent overdue notification for property: {PropertyCode} (Tag: {TagNumber})", 
                    property.PropertyCode, property.SerialNumber ?? property.PropertyCode);
            }

            // Mark properties as notified
            foreach (var property in propertiesToNotify)
            {
                await firebaseService.MarkOverdueNotificationSentAsync(property.Id);
                _logger.LogInformation("Marked overdue notification as sent for property: {PropertyCode}", property.PropertyCode);
            }

            _logger.LogInformation("Successfully sent overdue property notifications for {Count} property(ies)", 
                propertiesToNotify.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and notifying overdue properties");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OverduePropertyNotificationService is stopping.");
        await base.StopAsync(cancellationToken);
    }
}
