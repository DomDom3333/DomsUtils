# Hybrid Caches

Compositions built from multiple underlying caches.

## TieredCache<TKey,TValue>

Uses several caches in priority order and can migrate entries between tiers.
- `TryGet`, `Set`, `Remove`, `Clear`
- `TriggerMigrationNow()` forces migration according to rules.

## DirectionalTierCache<TKey,TValue>

Directional migration between tiers based on strategy and cache ordering.

## ParallelCache<TKey,TValue>

Writes to all caches in parallel and reads in priority order.

### Example

```csharp
var mem = new MemoryCache<string,int>();
var file = new FileCache<string,int>("./cache");
var tiered = new TieredCache<string,int>(new MigrationRuleSet<string,int>(), mem, file);

tiered.Set("a", 1);
if (tiered.TryGet("a", out int v))
    Console.WriteLine(v);
```

