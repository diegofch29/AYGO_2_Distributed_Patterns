using LoadBalancer.Models;

namespace LoadBalancer.Services;

public interface IRedisService
{
    Task AddServiceAsync(RegistrationModel service);
    Task<List<RegistrationModel>> GetAllServicesAsync();
    Task RemoveServiceAsync(string serviceId);
    Task<bool> ServiceExistsAsync(string serviceId);
    Task<RegistrationModel?> GetServiceAsync(string serviceId);
    Task UpdateServiceAsync(RegistrationModel service);
    Task<int> GetServiceCountAsync();
    Task<int> GetAndIncrementCounterAsync();
    Task ResetCounterAsync();
}