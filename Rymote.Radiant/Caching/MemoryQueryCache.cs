using System.Collections.Concurrent;
using System.Text.Json;

namespace Rymote.Radiant.Caching;

public class MemoryQueryCache : IQueryCache
{
    private readonly ConcurrentDictionary<string, CacheItem> cache = new();
    private readonly Timer cleanupTimer;

    public MemoryQueryCache()
    {
        cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (cache.TryGetValue(key, out CacheItem? item))
        {
            if (item.ExpiresAt == null || item.ExpiresAt > DateTime.UtcNow)
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(item.Value));
            }
            else
            {
                cache.TryRemove(key, out _);
            }
        }
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        DateTime? expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null;
        string serializedValue = JsonSerializer.Serialize(value);
        
        cache.AddOrUpdate(key, 
            new CacheItem { Value = serializedValue, ExpiresAt = expiresAt },
            (k, v) => new CacheItem { Value = serializedValue, ExpiresAt = expiresAt });
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        cache.Clear();
        return Task.CompletedTask;
    }

    private void CleanupExpiredItems(object? state)
    {
        DateTime now = DateTime.UtcNow;
        List<string> expiredKeys = new();

        foreach (KeyValuePair<string, CacheItem> kvp in cache)
        {
            if (kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt <= now)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (string key in expiredKeys)
        {
            cache.TryRemove(key, out _);
        }
    }

    private class CacheItem
    {
        public string Value { get; set; } = "";
        public DateTime? ExpiresAt { get; set; }
    }
}
