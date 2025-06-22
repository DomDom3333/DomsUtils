# Base Caches

Implementations that store data in specific mediums.

## FileCache<TKey,TValue>

Stores values as JSON files on disk.
- `TryGet(key, out value)`
- `Set(key, value)`
- `Remove(key)` and `Clear()`

## MemoryCache<TKey,TValue>

Thread safe in-memory dictionary with events.
- `TryGet`, `Set`, `Remove`, `Clear`
- `OnSet` event fires when values are updated.

## S3Cache<TKey,TValue>

Persists entries in an Amazon S3 bucket.
- `TryGet`, `Set`, `Remove`, `Clear`
- factory `Create` helpers for custom key converters.

### Example

```csharp
var cache = new MemoryCache<string,int>();
cache.Set("count", 5);
if (cache.TryGet("count", out int c))
    Console.WriteLine(c);
```

