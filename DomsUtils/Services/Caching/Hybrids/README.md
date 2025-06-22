# Hybrid Caches

Compositions built from multiple underlying caches. They allow combining fast in-memory caches with durable stores.

## TieredCache<TKey,TValue>
Uses several caches in priority order and can automatically migrate entries between tiers based on a `MigrationRuleSet`.

## DirectionalTierCache<TKey,TValue>
Migration always happens in one direction (promotion or demotion) following a configurable strategy.

## ParallelCache<TKey,TValue>
Writes to all caches in parallel and reads from them in priority order so that the fastest cache serves most requests.

### Example
```csharp
var mem = new MemoryCache<string,int>();
var file = new FileCache<string,int>("./cache");
var tiered = new TieredCache<string,int>(new MigrationRuleSet<string,int>(), mem, file);

tiered.Set("a", 1);
if (tiered.TryGet("a", out int v))
    Console.WriteLine(v);
```
