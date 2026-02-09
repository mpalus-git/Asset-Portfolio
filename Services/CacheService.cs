namespace PortfelStudenta.Services;
public interface ICacheService : IDisposable
{
    T? Get<T>(string key) where T : class;
    void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    void Remove(string key);
    void Clear();
    bool ContainsKey(string key);
}
public class MemoryCacheService : ICacheService
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly object _lock = new();
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(2);
    private readonly Timer _cleanupTimer;
    private bool _disposed;
    private sealed class CacheEntry
    {
        public object Value { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
    }
    public MemoryCacheService()
    {
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    private void CleanupExpiredEntries(object? state)
    {
        if (_disposed) return;
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.ExpiresAt <= now)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }
        }
    }
    public T? Get<T>(string key) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    return entry.Value as T;
                }
                _cache.Remove(key);
            }
            return null;
        }
    }
    public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_lock)
        {
            var expiresAt = DateTime.UtcNow.Add(expiration ?? _defaultExpiration);
            _cache[key] = new CacheEntry { Value = value, ExpiresAt = expiresAt };
        }
    }
    public void Remove(string key)
    {
        if (_disposed) return;
        lock (_lock)
        {
            _cache.Remove(key);
        }
    }
    public void Clear()
    {
        if (_disposed) return;
        lock (_lock)
        {
            _cache.Clear();
        }
    }
    public bool ContainsKey(string key)
    {
        if (_disposed) return false;
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                    return true;
                _cache.Remove(key);
            }
            return false;
        }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cleanupTimer.Dispose();
        lock (_lock)
        {
            _cache.Clear();
        }
        GC.SuppressFinalize(this);
    }
}