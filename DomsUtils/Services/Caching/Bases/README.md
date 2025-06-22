# Base Caches

Implementations that store data in specific mediums.

## FileCache<TKey,TValue>
Stores values as JSON files on disk. Keys are converted to filenames and a small metadata file keeps track of the mapping.

## MemoryCache<TKey,TValue>
Thread safe in-memory dictionary with events. Useful for unit tests or as the first tier in a hybrid cache.

## S3Cache<TKey,TValue>
Persists entries in an Amazon S3 bucket. Designed for server environments where distributed persistence is required.

### Example
```csharp
var cache = new MemoryCache<string,int>();
cache.Set("count", 5);
if (cache.TryGet("count", out int c))
    Console.WriteLine(c);
```
