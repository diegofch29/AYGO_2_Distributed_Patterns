using System.Text;
using System.Text.Json;

namespace LoadBalancer.Helper;

public interface IHttpClientHelper
{
    Task<T?> GetAsync<T>(string url);
    Task<string> GetStringAsync(string url);
    Task<T?> PostAsync<T>(string url, object? data = null);
    Task<string> PostStringAsync(string url, object? data = null);
    Task<T?> PostAsync<T>(string url, string jsonContent);
    Task<string> PostStringAsync(string url, string jsonContent);
    Task<T?> GetAsyncWithFallback<T>(string url);
}

public class HttpClientHelper : IHttpClientHelper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientHelper> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpClientHelper(HttpClient httpClient, ILogger<HttpClientHelper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            _logger.LogInformation("Making GET request to: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
                return default(T);

            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for GET {Url}", url);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed for GET {Url}", url);
            throw;
        }
    }

    public async Task<string> GetStringAsync(string url)
    {
        try
        {
            _logger.LogInformation("Making GET request to: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for GET {Url}", url);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string url, object? data = null)
    {
        var jsonContent = data != null ? JsonSerializer.Serialize(data, _jsonOptions) : "{}";
        return await PostAsync<T>(url, jsonContent);
    }

    public async Task<string> PostStringAsync(string url, object? data = null)
    {
        var jsonContent = data != null ? JsonSerializer.Serialize(data, _jsonOptions) : "{}";
        return await PostStringAsync(url, jsonContent);
    }

    public async Task<T?> PostAsync<T>(string url, string jsonContent)
    {
        try
        {
            _logger.LogInformation("Making POST request to: {Url}", url);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseContent))
                return default(T);

            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for POST {Url}", url);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed for POST {Url}", url);
            throw;
        }
    }

    public async Task<string> PostStringAsync(string url, string jsonContent)
    {
        try
        {
            _logger.LogInformation("Making POST request to: {Url}", url);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for POST {Url}", url);
            throw;
        }
    }

    public async Task<T?> GetAsyncWithFallback<T>(string url)
    {
        try
        {
            _logger.LogInformation("Making GET request with fallback to: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Raw response content: {Content}", content);

            if (string.IsNullOrEmpty(content))
                return default(T);

            // Try to deserialize as the expected type first
            try
            {
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            catch (JsonException)
            {
                // If that fails and T is a List<SomeType>, try to handle nested arrays
                var targetType = typeof(T);
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = targetType.GetGenericArguments()[0];
                    var nestedListType = typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(elementType));

                    var nestedResult = JsonSerializer.Deserialize(content, nestedListType, _jsonOptions);
                    if (nestedResult is System.Collections.IEnumerable enumerable)
                    {
                        var flatList = Activator.CreateInstance(targetType);
                        var addMethod = targetType.GetMethod("Add");

                        foreach (var innerList in enumerable)
                        {
                            if (innerList is System.Collections.IEnumerable innerEnumerable)
                            {
                                foreach (var item in innerEnumerable)
                                {
                                    addMethod?.Invoke(flatList, new[] { item });
                                }
                            }
                        }
                        return (T?)flatList;
                    }
                }
                throw;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for GET {Url}", url);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed for GET {Url}", url);
            throw;
        }
    }
}