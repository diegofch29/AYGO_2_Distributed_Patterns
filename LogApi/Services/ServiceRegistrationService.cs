using LogApi.Models;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LogApi.Services;

public class ServiceRegistrationService : IServiceRegistrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceRegistrationService> _logger;
    private readonly IConfiguration _configuration;
    private RegistrationModel? _currentRegistration;

    public ServiceRegistrationService(
        HttpClient httpClient,
        ILogger<ServiceRegistrationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<bool> RegisterServiceAsync(RegistrationModel? registration = null)
    {
        try
        {
            var loadBalancerUrl = _configuration.GetValue<string>("ServiceRegistration:LoadBalancerUrl");

            if (string.IsNullOrEmpty(loadBalancerUrl))
            {
                _logger.LogWarning("LoadBalancerUrl is not configured. Cannot register service.{loadBalancerUrl}", loadBalancerUrl);
                return false;
            }

            loadBalancerUrl = ValidateAndNormalizeUrl(loadBalancerUrl);
            if (string.IsNullOrEmpty(loadBalancerUrl))
            {
                _logger.LogError("LoadBalancerUrl is not a valid URL format");
                return false;
            }

            registration ??= GetCurrentRegistration();

            var json = JsonSerializer.Serialize(registration);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine("Registering service with payload: " + json);

            var response = await _httpClient.PostAsync($"{loadBalancerUrl}/api/Registration", content);

            if (response.IsSuccessStatusCode)
            {
                _currentRegistration = registration;
                _logger.LogInformation("Service registered successfully: {ServiceName} at {ServiceUrl}",
                    registration.ServiceName, registration.ServiceUrl);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to register service. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering service");
            return false;
        }
    }

    public async Task<bool> UnregisterServiceAsync(RegistrationModel? registration = null)
    {
        try
        {
            var loadBalancerUrl = _configuration.GetValue<string>("ServiceRegistration:LoadBalancerUrl");

            if (string.IsNullOrEmpty(loadBalancerUrl))
            {
                _logger.LogWarning("LoadBalancerUrl is not configured. Cannot unregister service.");
                return false;
            }

            loadBalancerUrl = ValidateAndNormalizeUrl(loadBalancerUrl);
            if (string.IsNullOrEmpty(loadBalancerUrl))
            {
                _logger.LogError("LoadBalancerUrl is not a valid URL format");
                return false;
            }

            registration ??= _currentRegistration ?? GetCurrentRegistration();

            var json = JsonSerializer.Serialize(registration);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{loadBalancerUrl}/api/unregister", content);

            if (response.IsSuccessStatusCode)
            {
                _currentRegistration = null;
                _logger.LogInformation("Service unregistered successfully: {ServiceName}",
                    registration.ServiceName);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to unregister service. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering service");
            return false;
        }
    }

    public async Task<bool> IsRegisteredAsync()
    {
        try
        {
            var loadBalancerUrl = _configuration.GetValue<string>("ServiceRegistration:LoadBalancerUrl");

            if (string.IsNullOrEmpty(loadBalancerUrl))
            {
                return false;
            }

            loadBalancerUrl = ValidateAndNormalizeUrl(loadBalancerUrl);
            if (string.IsNullOrEmpty(loadBalancerUrl))
            {
                _logger.LogError("LoadBalancerUrl is not a valid URL format");
                return false;
            }

            var registration = GetCurrentRegistration();
            var response = await _httpClient.GetAsync($"{loadBalancerUrl}/api/status/{registration.ServiceName}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking registration status");
            return false;
        }
    }

    public RegistrationModel GetCurrentRegistration()
    {
        if (_currentRegistration != null)
            return _currentRegistration;

        var serviceName = _configuration.GetValue<string>("ServiceRegistration:ServiceName") ?? "LogApi";
        var servicePort = _configuration.GetValue<string>("ServiceRegistration:ServicePort") ?? "5000";
        var ipAddress = _configuration.GetValue<string>("ServiceRegistration:ServiceIpAddress");
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = GetServiceIpAddress();
        }

        return new RegistrationModel
        {
            ServiceName = serviceName,
            ServiceUrl = $"http://{ipAddress}:{servicePort}",
            IpAddress = ipAddress
        };
    }

    public string GetServiceIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork
                                     && !IPAddress.IsLoopback(ip))?
                .ToString();

            if (!string.IsNullOrEmpty(ipAddress))
                return ipAddress;

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address?.ToString() ?? "127.0.0.1";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not determine IP address, using localhost");
            return "127.0.0.1";
        }
    }

    private string? ValidateAndNormalizeUrl(string url)
    {
        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "http://" + url;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.ToString().TrimEnd('/');
            }

            _logger.LogError("Invalid URL format: {Url}", url);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating URL: {Url}", url);
            return null;
        }
    }
}