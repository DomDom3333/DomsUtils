# BiMap Base

Core implementation of the bidirectional map and the `IBiMap<TKey,TValue>` interface.

`BiMap<TKey,TValue>` stores keys and values in two dictionaries ensuring each value is unique and allowing fast lookups in both directions. `IBiMap<TKey,TValue>` exposes the public API used by the base and observable implementations.

Key features

- Add single entries or ranges with duplicate checks
- Remove by key or by value
- Retrieve by key or by value or enumerate all pairs
- High‑performance span helpers such as `TryGetKeysToSpan`
- Async methods like `TryAddAsync` for background operations

### Example
```csharp
using DomsUtils.DataStructures.BiMap.Base;

IBiMap<int,string> map = new BiMap<int,string>();
map.Add(1, "one");
map.Add(2, "two");
int key = map["two"];        // 2
map.RemoveByKey(1);
```
