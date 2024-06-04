using LRUCache_Task_FINBOURNE.Models;
using LRUCache_Task_FINBOURNE.Utils;
using Microsoft.Extensions.Configuration;

namespace LRUCache_Task_FINBOURNE.Cache
{
    public class LRUCache<TKey, TValue> : ICache<TKey, TValue>
    {
        private static LRUCache<TKey, TValue> _instance;

        private readonly Dictionary<TKey, LinkedListNode<CacheItem<TKey, TValue>>> _cache = [];

        private readonly LinkedList<CacheItem<TKey, TValue>> _lruList = new();
        private readonly object _cacheLock = new object();
        private readonly object _evictionLock = new object();
        private bool _isEvicting = false;

        protected readonly IConfiguration _configuration;
        protected readonly Hysteresis _optimalItemSizeRange;

        private int _inCacheCount;
        private int _notInCacheCount;

        public delegate void EvictionHandler(TKey key, TValue value);
        public event EvictionHandler OnEviction;

        public double HitRate => _inCacheCount / (double)(_inCacheCount + _notInCacheCount);

        public LRUCache(IConfiguration configuration)
        {
            _configuration = configuration;

            var hysteresis = _configuration.GetValue<double>("Hysteresis");
            var itemThreshold = _configuration.GetValue<double>("ItemThreshold");

            var lowerThreshold = itemThreshold * (1 - hysteresis);
            var upperThreshold = itemThreshold * (1 + hysteresis);

            _optimalItemSizeRange = new Hysteresis(lowerThreshold, upperThreshold);

        }

        public static LRUCache<TKey, TValue> GetInstance(IConfiguration configuration)
        {
            if (_instance == null)
            {
                _instance = new LRUCache<TKey, TValue>(configuration);
            }
            return _instance;
        }

        //Assuming that if a match is not found a default "null" will be returned
        public TValue Get(TKey key)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    if (node.Value.Expiry < DateTime.UtcNow)
                    {
                        // Item has expired
                        RemoveNode(node);
                        _notInCacheCount++;
                        return default;
                    }

                    TValue value = node.Value.Value;

                    UpdateLRU(node);
                    _inCacheCount++;

                    return value;
                }
                else
                {
                    _notInCacheCount++;
                }
            }

            return default;
        }

        public void Add(TKey key, TValue value, TimeSpan ttl)
        {
            lock (_cacheLock)
            {
                _optimalItemSizeRange.Check(_cache.Count);

                if (_optimalItemSizeRange.State && _optimalItemSizeRange.AboveUpperThreshold && !_isEvicting)
                {
                    _isEvicting = true;
                    Task.Run(() => Evict());
                }

                if (_cache.TryGetValue(key, out var existingNode))
                {
                    existingNode.Value.Value = value;
                    existingNode.Value.Expiry = DateTime.UtcNow.Add(ttl);
                    UpdateLRU(existingNode);
                }
                else
                {
                    var cacheItem = new CacheItem<TKey, TValue>(key, value, DateTime.UtcNow.Add(ttl));
                    var node = new LinkedListNode<CacheItem<TKey, TValue>>(cacheItem);
                    _lruList.AddLast(node);
                    _cache[key] = node;
                }
            }
        }

        //Kept as a private method because we want to keep this logic only within the cache
        private void Evict()
        {
            lock (_evictionLock)
            {
                try
                {
                    while (_optimalItemSizeRange.State)
                    {
                        lock (_cacheLock)
                        {
                            RemoveFirst();
                            _optimalItemSizeRange.Check(_cache.Count);
                        }
                    }
                }
                finally
                {
                    _isEvicting = false;
                }
            }
        }

        private void RemoveFirst()
        {
            if (_lruList.First != null)
            {
                var node = _lruList.First;
                RemoveNode(node);
            }
        }

        private void RemoveNode(LinkedListNode<CacheItem<TKey, TValue>> node)
        {
            _lruList.Remove(node);
            _cache.Remove(node.Value.Key);

            OnEviction?.Invoke(node.Value.Key, node.Value.Value);
        }

        public void Update(TKey key)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    UpdateLRU(existingNode);
                }
            }
        }
        private void UpdateLRU(LinkedListNode<CacheItem<TKey, TValue>> node)
        {
            _lruList.Remove(node);
            _lruList.AddLast(node);
        }

        public int Size()
        {
            lock (_cacheLock)
            {
                return _cache.Count;
            }
        }
    }
}
