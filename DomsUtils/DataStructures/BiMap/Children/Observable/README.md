# ObservableBiMap

`ObservableBiMap<TKey,TValue>` extends `BiMap` with `INotifyCollectionChanged` and `INotifyPropertyChanged` support. It is useful when UI or other components need to react to changes in the map.

Typical use cases include WPF/WinUI data binding or any scenario where events are required when the collection mutates.

### Example
```csharp
using DomsUtils.DataStructures.BiMap.Children.Observable;

var map = new ObservableBiMap<int,string>();
map.CollectionChanged += (_, e) => Console.WriteLine($"action: {e.Action}");

map.Add(1, "one");   // event raised
map.RemoveByKey(1);   // event raised
```
