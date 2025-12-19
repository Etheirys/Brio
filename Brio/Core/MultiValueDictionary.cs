using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Brio.Core;

public class  MultiValueDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, List<TValue>> _underlyingDictionary = [];

    public IEnumerable<TKey> Keys => _underlyingDictionary.Keys;
    public int Count => _underlyingDictionary.Count;
    public void Add(TKey key, TValue val)
    {
        if(_underlyingDictionary.ContainsKey(key))
        {
            _underlyingDictionary[key].Add(val);
        }
        else
        {
            _underlyingDictionary.Add(key, [val]);
        }
    }
    public void Clear()
    {
        _underlyingDictionary.Clear();
    }
    public bool ContainsKey(TKey key)
    {
        return _underlyingDictionary.ContainsKey(key);
    }
    public void Remove(TKey key)
    {
        if(_underlyingDictionary.ContainsKey(key))
        {
            _underlyingDictionary.Remove(key);
        }
    }
    public void Remove(TKey key, TValue val)
    {
        if(_underlyingDictionary.TryGetValue(key, out var collection) && collection.Remove(val))
        {
            if(collection.Count == 0)
                _underlyingDictionary.Remove(key);
        }
    }
    public bool Contains(TKey key, TValue val)
    {
        return (_underlyingDictionary.TryGetValue(key, out var collection) && collection.Contains(val));
    }

    public bool TryGetValues(TKey key, [MaybeNullWhen(false)] out List<TValue> collection)
    {
        return _underlyingDictionary.TryGetValue(key, out collection);
    }

    public List<TValue> this[TKey key]
    {
        get
        {
            if(_underlyingDictionary.TryGetValue(key, out var collection))
                return collection;

            throw new KeyNotFoundException();
        }
    }
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach(var entry in _underlyingDictionary)
        {
            var collection = entry.Value;

            foreach(var item in collection)
            {
                yield return new KeyValuePair<TKey, TValue>(entry.Key, item);
            }
        }
    }

}
