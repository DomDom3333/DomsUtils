# Caching Services

This directory hosts a set of cache implementations plus helpers to combine them.
It ranges from simple in-memory storage to file and S3 persistence and supports hybrid setups where multiple caches cooperate.

## Subfolders
- [Bases](Bases/README.md) – file, memory and S3 based caches.
- [Hybrids](Hybrids/README.md) – tiered and parallel caches built from the base implementations.
- `Interfaces` – common contracts for caches and addons.
- `Addons` – migration rules used by tiered caches.

## Basic Usage
```csharp
using DomsUtils.Services.Caching.Bases;

ICache<string,int> cache = new MemoryCache<string,int>();
cache.Set("a", 1);
if (cache.TryGet("a", out int v))
    Console.WriteLine(v);
```
