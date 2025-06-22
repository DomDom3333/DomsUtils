# BiMap Utilities

This folder contains the bidirectional dictionary implementation and related helpers.

## Classes

- `BiMap<TKey,TValue>` – core bidirectional map enforcing one-to-one mapping. Key functions:
  - `Add(key, value)` / `AddRange(items)`
  - `RemoveByKey(key)` / `RemoveByValue(value)`
  - `TryAdd(key, value)` and async counterparts
  - `TryGetByKey(key, out value)` / `TryGetByValue(value, out key)`
  - Span helpers like `TryGetKeysToSpan` for high performance
- `ObservableBiMap<TKey,TValue>` – raises `CollectionChanged` and `PropertyChanged` events when the map changes.
- `BiMapExtensions` – convert dictionaries or enumerables into a `BiMap`.
- `BiMapJsonExtensions` – serialize and deserialize a map through `System.Text.Json`.

## Example

```csharp
using DomsUtils.DataStructures.BiMap.Base;
using DomsUtils.DataStructures.BiMap.Extensions;

// create map
var map = new BiMap<int, string>();
map.Add(1, "one");
map.AddRange(new[]{ new KeyValuePair<int,string>(2,"two") });

// lookup
if (map.TryGetByKey(1, out var value))
    Console.WriteLine(value); // "one"

// convert from dictionary
var dict = new Dictionary<int,string>{{3,"three"}};
BiMap<int,string> fromDict = dict.ToBiMap();

// JSON round trip
string json = fromDict.Serialize();
BiMap<int,string>? restored = BiMapJsonExtensions.Deserialize<int,string>(json);
```

