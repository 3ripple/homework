namespace UrlShort.Caching;

public class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _cache = new();
    private readonly LinkedList<Entry> _list = [];
    private readonly TimeSpan _ttl;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _lock = new();

    public LruCache(int capacity, TimeSpan ttl, TimeProvider? timeProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);
        
        _capacity = capacity;
        _ttl = ttl;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool TryGet(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var node))
            {
                value = default!;
                return false;
            }

            var expired = _timeProvider.GetElapsedTime(node.Value.TimeStamp, _timeProvider.GetTimestamp()) >= _ttl;
            if (expired)
            {
                _list.Remove(node);
                _cache.Remove(key);
                
                value = default!;
                return false;
            }
            _list.Remove(node);
            _list.AddFirst(node);
            
            value = node.Value.Value;
            return true;
        }
    }
    
    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            var now = _timeProvider.GetTimestamp();

            if (_cache.TryGetValue(key, out var node))
            {
                node.Value.Value = value;
                node.Value.TimeStamp = now;
                
                _list.Remove(node);
                _list.AddFirst(node);
                return;
            }

            if (_cache.Count >= _capacity)
            {
                var last = _list.Last;
                _cache.Remove(last!.Value.Key);
                _list.RemoveLast();
            }

            var entry = new Entry
            {
                Key = key,
                Value = value,
                TimeStamp = now,
            };
            var newNode = _list.AddFirst(entry);
            _cache[key] = newNode;
        }
    }

    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var node))
                return false;
            _list.Remove(node);
            _cache.Remove(key);
            return true;
        }
    }
    
    private class Entry
    {
        public required TKey Key {get; init;}
        public required TValue Value {get; set;}
        public required long TimeStamp {get; set;}
    }
}