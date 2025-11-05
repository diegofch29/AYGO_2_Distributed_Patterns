namespace LogApi.Model;

public class ServiceRegistrationModel
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class ServiceRegistrationRequest
{
    public string? ServiceName { get; set; }
    public string? ServiceUrl { get; set; }
    public string? IpAddress { get; set; }
}

public class ServiceRegistrationStatusResponse
{
    public bool IsRegistered { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string LoadBalancerUrl { get; set; } = string.Empty;
    public DateTime? LastRegistration { get; set; }
}