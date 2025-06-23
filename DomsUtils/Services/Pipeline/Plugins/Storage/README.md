# Block Storage for ChannelPipeline

The Block Storage module provides a way for pipeline blocks to store and retrieve data across different processing stages. This enables scenarios like caching, shared state, context propagation, and more.

## Core Components

### IBlockStorage<TKey, TValue>

The fundamental interface for block storage implementations:

```csharp
public interface IBlockStorage<TKey, TValue>
{
    bool TryGetValue(TKey key, out TValue? value);
    void SetValue(TKey key, TValue value);
    bool RemoveValue(TKey key);
    void Clear();
}
```

### InMemoryBlockStorage<TKey, TValue>

Default implementation using a thread-safe dictionary:

```csharp
public class InMemoryBlockStorage<TKey, TValue> : IBlockStorage<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _storage = new();

    // Implementation methods...
}
```

### StoragePlugin<T, TKey, TValue>

A plugin that provides storage to a pipeline:

```csharp
public class StoragePlugin<T, TKey, TValue> : IPipelinePlugin<T> where TKey : notnull
{
    private readonly IBlockStorage<TKey, TValue> _storage;

    public IBlockStorage<TKey, TValue> Storage => _storage;

    // Implementation methods...
}
```

### Block Modifiers

Block modifiers that interact with storage:

```csharp
public static class StorageBlockModifiers
{
    // Store results in storage
    public static BlockModifier<T> StoreResult<T, TKey>(...) { ... }

    // Use stored data during processing
    public static BlockModifier<T> WithStoredData<T, TKey, TValue>(...) { ... }

    // Skip processing based on storage state
    public static BlockModifier<T> SkipIfStored<T, TKey, TValue>(...) { ... }
}
```

## Common Use Cases

### Caching Expensive Results

Use storage to cache results of expensive operations, preventing duplicate work:

```csharp
var pipeline = new ChannelPipeline<MyData>()
    .WithStorage<MyData, string, ProcessedResult>()
    .AddBlock(new BlockOptions<MyData>
    {
        AsyncTransform = async (data, ct) =>
        {
            // Expensive operation
            return await ExpensiveOperation(data, ct);
        },
        Modifiers = new BlockModifier<MyData>[]
        {
            // Skip if already in cache
            StorageBlockModifiers.SkipIfStored<MyData, string, ProcessedResult>(
                pipeline,
                data => data.Id,
                cached => cached != null
            ),
            // Store in cache
            StorageBlockModifiers.StoreResult<MyData, string>(
                pipeline,
                data => data.Id
            )
        }
    });
```

### Accumulating State

Build up state across multiple blocks:

```csharp
var pipeline = new ChannelPipeline<Order>()
    .WithStorage<Order, string, decimal>()
    .AddBlock(new BlockOptions<Order>
    {
        AsyncTransform = async (order, ct) =>
        {
            var storage = pipeline.GetStorage<Order, string, decimal>();

            // Update running total
            storage.TryGetValue("total", out decimal total);
            storage.SetValue("total", total + order.Amount);

            return order;
        }
    })
    .AddBlock(new BlockOptions<Order>
    {
        AsyncTransform = async (order, ct) =>
        {
            var storage = pipeline.GetStorage<Order, string, decimal>();
            storage.TryGetValue("total", out decimal total);

            // Attach running total to the order
            order.RunningTotal = total;
            return order;
        }
    });
```

### Creating Persistent Storage

Implement custom storage that persists data:

```csharp
public class FileBackedStorage<TKey, TValue> : IBlockStorage<TKey, TValue>, IDisposable
    where TKey : notnull
    where TValue : class
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<TKey, TValue> _cache = new();

    public FileBackedStorage(string filePath)
    {
        _filePath = filePath;

        // Load from file if exists
        if (File.Exists(filePath))
        {
            // Deserialize from file
        }
    }

    public void Dispose()
    {
        // Save to file
    }

    // Implementation methods...
}
```

## Integration with Other Systems

The `IBlockStorage` interface can be implemented to integrate with various external systems:

- **Databases**: Store and retrieve data from SQL or NoSQL databases
- **Caching Systems**: Redis, Memcached, or other distributed caches
- **In-Memory Data Grids**: Hazelcast, Ignite, or other IMDGs
- **Filesystem**: Store data in files for persistence between runs

This allows the pipeline to maintain state across process restarts or share state across distributed pipeline instances.
