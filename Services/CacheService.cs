// Services/CacheService.cs
using System.Collections.Concurrent;
using Poe2ScoutPricer.Utils;

namespace Poe2ScoutPricer.Services
{
    public interface ICacheService : IDisposable
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        void Remove(string key);
        void Clear();
        bool TryGet<T>(string key, out T? value);
    }

    public class CacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
        private readonly Timer _cleanupTimer;
        private bool _disposed = false;

        public CacheService()
        {
            // Cleanup expired items every 5 minutes
            _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public T? Get<T>(string key)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CacheService));

            if (TryGet<T>(key, out var value))
                return value;
            return default;
        }

        public bool TryGet<T>(string key, out T? value)
        {
            value = default;

            if (_disposed)
                return false;

            if (!_cache.TryGetValue(key, out var cacheItem))
                return false;

            if (cacheItem.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return false;
            }

            try
            {
                value = (T?)cacheItem.Value;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to cast cached value for key '{key}': {ex.Message}");
                _cache.TryRemove(key, out _);
                return false;
            }
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CacheService));

            var expirationTime = expiration.HasValue 
                ? DateTime.UtcNow.Add(expiration.Value) 
                : DateTime.UtcNow.AddHours(1); // Default 1 hour

            var cacheItem = new CacheItem
            {
                Value = value,
                ExpirationTime = expirationTime
            };

            _cache.AddOrUpdate(key, cacheItem, (_, _) => cacheItem);
            Logger.LogDebug($"Cached item with key '{key}', expires at {expirationTime}");
        }

        public void Remove(string key)
        {
            if (_disposed)
                return;

            _cache.TryRemove(key, out _);
            Logger.LogDebug($"Removed cached item with key '{key}'");
        }

        public void Clear()
        {
            if (_disposed)
                return;

            _cache.Clear();
            Logger.LogInfo("Cache cleared");
        }

        private void CleanupExpiredItems(object? state)
        {
            if (_disposed)
                return;

            var expiredKeys = new List<string>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                Logger.LogDebug($"Cleaned up {expiredKeys.Count} expired cache items");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cleanupTimer?.Dispose();
                _cache.Clear();
                _disposed = true;
                Logger.LogInfo("CacheService disposed");
            }
        }

        private class CacheItem
        {
            public object? Value { get; set; }
            public DateTime ExpirationTime { get; set; }
            public bool IsExpired => DateTime.UtcNow > ExpirationTime;
        }
    }
}