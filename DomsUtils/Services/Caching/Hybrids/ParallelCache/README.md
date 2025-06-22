# ParallelCache

Wraps multiple caches and writes to all of them in parallel. Reads happen in priority order and the first cache providing a value wins.

Useful when combining a fast local cache with a slower distributed cache while keeping them in sync.

### Example
```csharp
var mem = new MemoryCache<string,int>();
var file = new FileCache<string,int>("./cache");
var cache = new ParallelCache<string,int>(new[]{mem,file});
await cache.SetAsync("x", 42);
```
