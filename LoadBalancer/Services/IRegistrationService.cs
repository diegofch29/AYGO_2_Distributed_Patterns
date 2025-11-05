using LoadBalancer.Models;

namespace LoadBalancer.Services;

public interface IRegistrationService
{
    Task RegisterServiceAsync(RegistrationModel service);
    List<string> GetRegisteredServices();
    string GetNextServiceRoundRobin();
}