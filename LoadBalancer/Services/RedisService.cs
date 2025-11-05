using LoadBalancer.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LoadBalancer.Services;

public class RedisService : IRedisService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisService> _logger;
    private const string SERVICES_KEY = "registered_services";
    private const string COUNTER_KEY = "round_robin_counter";

    public RedisService(IDistributedCache cache, ILogger<RedisService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task AddServiceAsync(RegistrationModel service)
    {
        try
        {
            var services = await GetAllServicesAsync();

            // Check if service already exists (by ServiceName or ServiceUrl)
            var existingService = services.FirstOrDefault(s =>
                s.ServiceName == service.ServiceName || s.ServiceUrl == service.ServiceUrl);

            if (existingService != null)
            {
                _logger.LogInformation("Service {ServiceName} already exists, updating...", service.ServiceName);
                await UpdateServiceAsync(service);
                return;
            }

            services.Add(service);
            await SaveServicesAsync(services);
            _logger.LogInformation("Service {ServiceName} added to Redis", service.ServiceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding service {ServiceName} to Redis", service.ServiceName);
            throw;
        }
    }

    public async Task<List<RegistrationModel>> GetAllServicesAsync()
    {
        try
        {
            var servicesJson = await _cache.GetStringAsync(SERVICES_KEY);
            if (string.IsNullOrEmpty(servicesJson))
            {
                return new List<RegistrationModel>();
            }

            var services = JsonSerializer.Deserialize<List<RegistrationModel>>(servicesJson);
            return services ?? new List<RegistrationModel>();
        }
        catch (StackExchange.Redis.RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection failed. Returning empty service list.");
            return new List<RegistrationModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving services from Redis");
            return new List<RegistrationModel>();
        }
    }

    public async Task RemoveServiceAsync(string serviceId)
    {
        try
        {
            var services = await GetAllServicesAsync();
            var serviceToRemove = services.FirstOrDefault(s =>
                s.ServiceName == serviceId || s.ServiceUrl == serviceId);

            if (serviceToRemove != null)
            {
                services.Remove(serviceToRemove);
                await SaveServicesAsync(services);
                _logger.LogInformation("Service {ServiceId} removed from Redis", serviceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing service {ServiceId} from Redis", serviceId);
            throw;
        }
    }

    public async Task<bool> ServiceExistsAsync(string serviceId)
    {
        try
        {
            var services = await GetAllServicesAsync();
            return services.Any(s => s.ServiceName == serviceId || s.ServiceUrl == serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if service {ServiceId} exists in Redis", serviceId);
            return false;
        }
    }

    public async Task<RegistrationModel?> GetServiceAsync(string serviceId)
    {
        try
        {
            var services = await GetAllServicesAsync();
            return services.FirstOrDefault(s => s.ServiceName == serviceId || s.ServiceUrl == serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service {ServiceId} from Redis", serviceId);
            return null;
        }
    }

    public async Task UpdateServiceAsync(RegistrationModel service)
    {
        try
        {
            var services = await GetAllServicesAsync();
            var existingServiceIndex = services.FindIndex(s =>
                s.ServiceName == service.ServiceName || s.ServiceUrl == service.ServiceUrl);

            if (existingServiceIndex >= 0)
            {
                services[existingServiceIndex] = service;
                await SaveServicesAsync(services);
                _logger.LogInformation("Service {ServiceName} updated in Redis", service.ServiceName);
            }
            else
            {
                await AddServiceAsync(service);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service {ServiceName} in Redis", service.ServiceName);
            throw;
        }
    }

    public async Task<int> GetServiceCountAsync()
    {
        try
        {
            var services = await GetAllServicesAsync();
            return services.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service count from Redis");
            return 0;
        }
    }

    public async Task<int> GetAndIncrementCounterAsync()
    {
        try
        {
            var counterJson = await _cache.GetStringAsync(COUNTER_KEY);
            int counter = 0;

            if (!string.IsNullOrEmpty(counterJson))
            {
                counter = JsonSerializer.Deserialize<int>(counterJson);
            }

            var serviceCount = await GetServiceCountAsync();
            if (serviceCount == 0)
            {
                throw new InvalidOperationException("No registered services available.");
            }

            var currentCounter = counter;
            var nextCounter = (counter + 1) % serviceCount;

            await _cache.SetStringAsync(COUNTER_KEY, JsonSerializer.Serialize(nextCounter));

            return currentCounter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting and incrementing counter in Redis");
            throw;
        }
    }

    public async Task ResetCounterAsync()
    {
        try
        {
            await _cache.SetStringAsync(COUNTER_KEY, JsonSerializer.Serialize(0));
            _logger.LogInformation("Counter reset in Redis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting counter in Redis");
            throw;
        }
    }

    private async Task SaveServicesAsync(List<RegistrationModel> services)
    {
        var servicesJson = JsonSerializer.Serialize(services);
        await _cache.SetStringAsync(SERVICES_KEY, servicesJson);
    }
}