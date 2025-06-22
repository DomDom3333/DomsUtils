# BiMap JSON Tooling

Contains the custom `System.Text.Json` converter used to serialize and deserialize `BiMap` instances.

`BiMapJsonConverterFactory` automatically supplies a typed converter for any `BiMap<TKey,TValue>` so regular `JsonSerializer` calls work out of the box.

### Example
```csharp
using System.Text.Json;
using DomsUtils.DataStructures.BiMap.Base;

var map = new BiMap<int,string>();
map.Add(1, "one");
string json = JsonSerializer.Serialize(map);       // uses converter
BiMap<int,string>? back = JsonSerializer.Deserialize<BiMap<int,string>>(json);
```
