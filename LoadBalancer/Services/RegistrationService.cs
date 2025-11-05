using LoadBalancer.Helper;
using LoadBalancer.Models;
using LoadBalancer.Services;

namespace LoadBalancer.Services;

public class RegistrationService : IRegistrationService
{
    private readonly ILogger<RegistrationService> _logger;
    private readonly IHttpClientHelper _httpClientHelper;
    private readonly IRedisService _redisService;

    public RegistrationService(ILogger<RegistrationService> logger, IHttpClientHelper httpClientHelper, IRedisService redisService)
    {
        _logger = logger;
        _httpClientHelper = httpClientHelper;
        _redisService = redisService;
    }

    public async Task RegisterServiceAsync(RegistrationModel service)
    {
        _logger.LogInformation("Registering service: {Service}", service.ServiceName);

        await _redisService.AddServiceAsync(service);

        _logger.LogInformation("Service {Service} registered successfully", service.ServiceName);
    }

    public List<string> GetRegisteredServices()
    {
        var services = _redisService.GetAllServicesAsync().GetAwaiter().GetResult();
        return services.Select(s => s.ServiceUrl).ToList();
    }

    private async Task HeartbeatAsync()
    {
        var services = await _redisService.GetAllServicesAsync();

        foreach (var service in services)
        {
            _logger.LogInformation("Sending heartbeat to service: {Service}", service);
            try
            {
                var heartbeatResponse = await _httpClientHelper.PostAsync<HeartbeatResponse>($"{service.ServiceUrl}/api/heartbeat", new { });
                _logger.LogInformation("Heartbeat response from {Service}: {Status}", service.ServiceUrl, heartbeatResponse?.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send heartbeat to service: {Service}", service.ServiceUrl);
                // Consider removing unhealthy services
                // await _redisService.RemoveServiceAsync(service.ServiceName);
            }
        }
    }

    public string GetNextServiceRoundRobin()
    {
        var services = _redisService.GetAllServicesAsync().GetAwaiter().GetResult();

        if (services.Count == 0)
        {
            throw new InvalidOperationException("No registered services available.");
        }

        var counterIndex = _redisService.GetAndIncrementCounterAsync().GetAwaiter().GetResult();
        return services[counterIndex].ServiceUrl;
    }
}