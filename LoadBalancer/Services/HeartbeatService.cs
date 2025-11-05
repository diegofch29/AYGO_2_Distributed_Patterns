using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class HeartbeatService : BackgroundService
{
    private readonly ILogger<HeartbeatService> _logger;
    private readonly ServiceRegistry _registry;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    public HeartbeatService(ILogger<HeartbeatService> logger, ServiceRegistry registry)
    {
        _logger = logger;
        _registry = registry;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3)
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var services = _registry.GetAll();
            foreach (var serviceUrl in services)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{serviceUrl}/health", stoppingToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Service {ServiceUrl} failed heartbeat (status {StatusCode}). Removing.", serviceUrl, response.StatusCode);
                        _registry.RemoveService(serviceUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to reach {ServiceUrl}. Removing from registry.", serviceUrl);
                    _registry.RemoveService(serviceUrl);
                }
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
