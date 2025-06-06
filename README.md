# DomsUtils

A collection of .NET utility classes providing bidirectional dictionaries, observable variants, extension methods, and JSON serialization support—features not commonly found in standard libraries.

## Table of Contents

- [Features](#features)  
- [Installation](#installation)  
- [Usage](#usage)  
  - [BiMap](#bimap)  
  - [ObservableBiMap](#observablebimap)  
  - [Extension Methods](#extension-methods)  
  - [JSON Serialization](#json-serialization)
  - [Caching](#caching)
- [API Reference](#api-reference)  
- [Testing](#testing)  
- [Contributing](#contributing)  
- [License](#license)  

## Features

- **BiMap\<TKey, TValue\>**  
  A bidirectional map enforcing a one-to-one correspondence between keys and values.  
- **ObservableBiMap\<TKey, TValue\>**  
  Extends `BiMap` with `INotifyCollectionChanged` and `INotifyPropertyChanged` for real-time change notifications.  
- **Extension Methods**  
  - `IDictionary<TKey,TValue>.ToBiMap()` for direct conversion.  
  - `ToBiMapSafe()` overloads with conflict-resolution callbacks.  
  - `IEnumerable<T>.ToBiMap()` overloads mapping sequences to bi-maps.  
- **JSON Support**  
  - `BiMapJsonExtensions.Serialize()` / `.Deserialize()` for easy JSON round-trip.
  - Custom `JsonConverterFactory`/`JsonConverter` preserving bijectivity.
- **Caching**
  - `MemoryCache<TKey,TValue>` for basic in-memory storage.
  - `TimestampedMemoryCache<TKey,TValue>` adds timestamp tracking for each entry.
  - `FileCache<TKey,TValue>` provides on-disk persistence.
  - `S3Cache<TKey,TValue>` integrates Amazon S3 as a backing store.
  - Hybrid caches (`TieredCache`, `ParallelCache`, `DirectionalTierCache`, `TimeBasedHybridCache`) combine multiple caches for advanced scenarios.

## Installation

Requires **.NET 9.0** or later.

1. **Clone & build**  
   ```bash
   git clone https://gitea.essenhofer.at/DomDom3333/DomsUtils.git
   cd DomsUtils
   dotnet build

2. **Reference**

    * Add a project reference to `DomsUtils/DomsUtils.csproj`, or
    * Copy the compiled `DomsUtils.dll` into your project.

## Usage

### BiMap

```csharp
using DomsUtils.Classes.BiMap.Base;

// Create and populate
var biMap = new BiMap<int, string>();
biMap.Add(1, "One");
biMap.Add(2, "Two");

// Lookup by key
Console.WriteLine(biMap[1]);      // "One"

// Lookup by value
Console.WriteLine(biMap["Two"]);  // 2

// Safe add
if (!biMap.TryAdd(2, "Second"))
    Console.WriteLine("Duplicate key or value");

// Remove entries
biMap.RemoveByKey(1);
biMap.RemoveByValue("Two");
```

Key members:

| Member                   | Description                                           |
| ------------------------ | ----------------------------------------------------- |
| `Count`                  | Number of entries                                     |
| `Keys`, `Values`         | Enumerables of all keys or values                     |
| `ContainsKey(key)`       | Tests for existence of a key                          |
| `ContainsValue(value)`   | Tests for existence of a value                        |
| `TryGetByKey/Value(...)` | Try-pattern lookup without exceptions                 |
| `Clear()`                | Removes all entries                                   |
| Indexers (`this[key]`)   | Throws `KeyNotFoundException` on missing key or value |

### ObservableBiMap

```csharp
using DomsUtils.Classes.BiMap.Children.Observable;
using System.Collections.Specialized;
using System.ComponentModel;

var obsMap = new ObservableBiMap<string, int>();

obsMap.CollectionChanged += (s, e) =>
    Console.WriteLine($"Collection changed: {e.Action}");

obsMap.PropertyChanged += (s, e) =>
    Console.WriteLine($"Property {e.PropertyName} changed");

// Triggers notifications
obsMap.Add("A", 1);
obsMap.RemoveByKey("A");
```

Supports all `BiMap` operations plus:

* **INotifyCollectionChanged**
* **INotifyPropertyChanged**

### Extension Methods

Convert standard collections into `BiMap`:

```csharp
using DomsUtils.Classes.BiMap.Extensions;

// Dictionary → BiMap
var dict = new Dictionary<int,string>{{1,"One"},{2,"Two"}};
var map1 = dict.ToBiMap();

// Safe conversion with conflict resolver
var map2 = dict.ToBiMapSafe((key, val) => {
    // Return true to overwrite existing entry
    return key % 2 == 0;
});

// Enumerable → BiMap via selectors
var words = new[] { "alpha", "beta", "gamma" };
var map3 = words.ToBiMap(
    word => word,         // key selector
    word => word.Length); // value selector
```

### JSON Serialization

Leverage built-in JSON support:

```csharp
using System.Text.Json;
using DomsUtils.Classes.BiMap.Extensions;

var biMap = new BiMap<int,string> { {1,"One"}, {2,"Two"} };

// Serialize to JSON
string json = biMap.Serialize(new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);

// Deserialize back
var restored = BiMapJsonExtensions.Deserialize<int,string>(json);
```

Under the hood, a custom `JsonConverterFactory` ensures no duplicate values are introduced during deserialization.

### Caching

`TimestampedMemoryCache` behaves like a regular in-memory cache but also stores a
timestamp for each entry:

```csharp
using DomsUtils.Services.Caching.Bases;

var cache = new TimestampedMemoryCache<string, int>();
cache.Set("user", 42);               // timestamp recorded automatically

if (cache.TryGetWithTimestamp("user", out var value, out var ts))
    Console.WriteLine($"{value} added at {ts}");
```

Hybrid caches such as `TieredCache` can subscribe to `OnSet` events from this
cache to migrate entries between tiers based on custom rules.

`TimeBasedHybridCache` automatically demotes entries from memory to a persistent
cache after they exceed a specified age:

```csharp
using DomsUtils.Services.Caching.Hybrids;

var memory = new TimestampedMemoryCache<string,int>();
var persistent = new FileCache<string,int>("/tmp/cache");

var hybrid = new TimeBasedHybridCache<string,int>(memory, persistent,
    TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));

hybrid.Set("session", 123);
hybrid.TriggerMigrationNow(); // manually run migration check
```

## API Reference

* **Namespace**: `DomsUtils.Classes.BiMap.Base`

    * `BiMap<TKey,TValue>`
    * `IBiMap<TKey,TValue>`
* **Namespace**: `DomsUtils.Classes.BiMap.Children.Observable`

    * `ObservableBiMap<TKey,TValue>`
* **Namespace**: `DomsUtils.Classes.BiMap.Extensions`

    * `BiMapExtensions`
    * `BiMapJsonExtensions`
* **Namespace**: `DomsUtils.Classes.BiMap.Base.Tooling`

    * `BiMapJsonConverterFactory`
    * `BiMapJsonConverter<TKey,TValue>`

> For detailed API signatures and XML docs, consult the source code or generate documentation with your preferred tool.

## Testing

Unit tests cover construction, add/remove operations, lookups, enumeration, custom comparers, async operations, and JSON round-trip.

```bash
cd DomsUtils.Tests
dotnet test
```

## Contributing

Contributions are welcome!

1. Fork the repository.
2. Create a feature branch.
3. Add tests for new functionality.
4. Submit a pull request.

Please adhere to the existing coding style and include XML doc comments for any new public members.

## [License](License.md)