# DirectionalTierCache

A tiered cache with a defined migration direction. Each tier is checked in order and data can move only in the configured direction (up or down) when the `MigrationStrategy` predicate is satisfied.

Useful for scenarios like promoting hot items from disk to memory or demoting rarely used entries.

### Example
```csharp
using DomsUtils.Services.Caching.Bases;
using DomsUtils.Services.Caching.Hybrids.DirectionalTierCache;

var tiers = new ICache<string,int>[]
{
    new MemoryCache<string,int>(),
    new FileCache<string,int>("./cache")
};
var cache = new DirectionalTierCache<string,int>(tiers);
cache.Set("a", 1);   // stored in first tier
```
