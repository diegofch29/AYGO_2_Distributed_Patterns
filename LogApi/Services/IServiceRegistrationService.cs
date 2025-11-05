using LogApi.Models;

namespace LogApi.Services;

public interface IServiceRegistrationService
{
    Task<bool> RegisterServiceAsync(RegistrationModel? registration = null);
    Task<bool> UnregisterServiceAsync(RegistrationModel? registration = null);
    Task<bool> IsRegisteredAsync();
    RegistrationModel GetCurrentRegistration();
    string GetServiceIpAddress();
}
