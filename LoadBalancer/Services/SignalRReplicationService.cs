using Microsoft.AspNetCore.SignalR;
using LoadBalancer.Hubs;
using LoadBalancer.Models;

namespace LoadBalancer.Services
{
    public class SignalRReplicationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SignalRReplicationService> _logger;

        public SignalRReplicationService(
            IServiceProvider serviceProvider,
            ILogger<SignalRReplicationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SignalR Replication Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // This service can be used for periodic tasks or monitoring
                    // For now, it just runs in the background
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("SignalR Replication Service is stopping.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in SignalR Replication Service.");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }

        public async Task NotifyLogAdded(LogDataModel logData)
        {
            using var scope = _serviceProvider.CreateScope();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ReplicationHub>>();

            try
            {
                Console.WriteLine($"Notifying ReplicationHub of log added: {logData}");
                await hubContext.Clients.All.SendAsync("LogAdded", logData);
                _logger.LogDebug("Log notification sent to ReplicationHub clients.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send log notification to ReplicationHub clients.");
            }
        }
    }
}