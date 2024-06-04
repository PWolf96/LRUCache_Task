namespace LRUCache_Task_FINBOURNE.Models
{
    public class CacheItem<TKey, TValue>
    {
        public CacheItem(TKey k, TValue v, DateTime expiry)
        {
            Key = k;
            Value = v;
            Expiry = expiry;
        }

        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public DateTime Expiry { get; set; }
    }
}
