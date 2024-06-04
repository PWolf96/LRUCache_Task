namespace LRUCache_Task_FINBOURNE.Cache
{
    public interface ICache<TKey, TValue>
    {
        TValue Get(TKey key);
        void Add(TKey key, TValue value, TimeSpan ttl);
        void Update(TKey key);
        int Size();
    }
}

