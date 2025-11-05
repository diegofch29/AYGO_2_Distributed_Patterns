namespace LoadBalancer.Models;

public class LogDataModel
{
    public string Name { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}