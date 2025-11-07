using Microsoft.AspNetCore.SignalR;
using LoadBalancer.Hubs;
using LoadBalancer.Models;

namespace LoadBalancer.Services
{
    public class LogNotificationService : ILogNotificationService
    {
        private readonly IHubContext<ReplicationHub> _hubContext;
        private readonly ILogger<LogNotificationService> _logger;

        public LogNotificationService(
            IHubContext<ReplicationHub> hubContext,
            ILogger<LogNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyLogAdded(LogDataModel logData)
        {
            try
            {
                Console.WriteLine($"Notifying ReplicationHub of log added: {logData}");
                await _hubContext.Clients.All.SendAsync("LogAdded", logData);
                _logger.LogDebug("Log notification sent to ReplicationHub clients.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send log notification to ReplicationHub clients.");
            }
        }
    }
}