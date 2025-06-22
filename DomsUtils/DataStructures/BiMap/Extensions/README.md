# BiMap Extensions

Helpers for converting collections to `BiMap` and for JSON serialization.

## Conversion Helpers
- `ToBiMap` and `ToBiMapSafe` convert dictionaries or enumerables to a `BiMap`
- `ToBiMapSafe` accepts a conflict resolver lambda for duplicate handling

## JSON Helpers
- `Serialize()` and `Deserialize()` extension methods use System.Text.Json
- Works together with the custom converter in `Base/Tooling`

### Example
```csharp
using DomsUtils.DataStructures.BiMap.Extensions;

var dict = new Dictionary<int,string>{{1,"one"}};
BiMap<int,string> map = dict.ToBiMap();
string json = map.Serialize();
BiMap<int,string>? restored = BiMapJsonExtensions.Deserialize<int,string>(json);
```
