using LoadBalancer.Services;

namespace LoadBalancer.Services;

public class RedisHealthService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RedisHealthService> _logger;
    private Timer? _timer;

    public RedisHealthService(IServiceProvider serviceProvider, ILogger<RedisHealthService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Redis Health Service started");

        // Check Redis connectivity every 30 seconds
        _timer = new Timer(CheckRedisHealth, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Redis Health Service stopping");
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async void CheckRedisHealth(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();

            // Simple health check by getting service count
            var count = await redisService.GetServiceCountAsync();
            _logger.LogDebug("Redis health check passed. Service count: {Count}", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
        }
    }
}