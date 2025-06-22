# Caching Services

This directory provides cache implementations and combinators.

## Subfolders

- [Bases](Bases/README.md) – file, memory and S3 based caches.
- [Hybrids](Hybrids/README.md) – multi-cache combinations like tiered and parallel caches.
- `Interfaces` – common contracts for caches and addons.
- `Addons` – migration rule helpers.

## Basic Usage

```csharp
using DomsUtils.Services.Caching.Bases;

ICache<string,int> cache = new MemoryCache<string,int>();
cache.Set("a", 1);
if (cache.TryGet("a", out int v))
    Console.WriteLine(v);
```

