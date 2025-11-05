public class ServiceRegistry
{
    private readonly List<string> _services = new();
    private readonly object _lock = new();

    public void RegisterService(string url)
    {
        lock (_lock)
        {
            if (!_services.Contains(url))
                _services.Add(url);
        }
    }

    public void RemoveService(string url)
    {
        lock (_lock)
        {
            _services.Remove(url);
        }
    }

    public List<string> GetAll()
    {
        lock (_lock)
        {
            return _services.ToList();
        }
    }
}
