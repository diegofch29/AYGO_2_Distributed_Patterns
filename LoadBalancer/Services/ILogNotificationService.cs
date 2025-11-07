using LoadBalancer.Models;

namespace LoadBalancer.Services
{
    public interface ILogNotificationService
    {
        Task NotifyLogAdded(LogDataModel logData);
    }
}