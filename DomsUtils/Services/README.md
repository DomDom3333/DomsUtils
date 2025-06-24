# ChannelPipeline
# ChannelPipeline

A composable channel-based pipeline for asynchronous data processing with support for parallelism, ordering, resilience patterns, and plugins. This library uses System.Threading.Channels as the underlying infrastructure for efficient asynchronous data processing.

## What is ChannelPipeline?

ChannelPipeline is a flexible framework for building data processing pipelines that:

- **Process data asynchronously** using modern .NET async/await patterns
- **Scale processing** with configurable parallelism for compute-intensive operations
- **Preserve ordering** (optionally) even when processing in parallel
- **Add resilience** with modifiers like retry, timeout, and circuit breakers
- **Handle errors gracefully** at each processing stage
- **Chain transformations** with a fluent API for readable pipeline construction
- **Share data between blocks** using storage plugins
- **Extend functionality** with a modular plugin system

## Core Components

### Building Blocks
- `ChannelPipeline<T>` – The main pipeline orchestrator that manages execution flow and resource lifecycle
- `BlockOptions<T>` – Configuration for each processing block, including transformation logic and execution settings
- `BlockModifier<T>` – Middleware-like wrappers that enhance blocks with cross-cutting concerns like retries or logging
- `Envelope<T>` – Internal wrapper that tracks sequencing information for maintaining order when needed

### Plugin System
- `IPipelinePlugin<T>` - Interface for creating plugins that extend pipeline functionality
- `StoragePlugin<T, TKey, TValue>` - Built-in plugin for adding shared storage to a pipeline

### Block Storage
- `IBlockStorage<TKey, TValue>` - Interface for creating storage implementations
- `InMemoryBlockStorage<TKey, TValue>` - Default in-memory implementation of block storage
- `StorageBlockModifiers` - Collection of modifiers for interacting with storage

## Using Block Storage

Block storage allows data to be shared between different blocks in a pipeline. This is useful for:

- Caching expensive computation results
- Sharing context between blocks
- Building up state during processing
- Implementing deduplication or memoization

### Adding Storage to a Pipeline

```csharp
// Method 1: Using extension method
var pipeline = new ChannelPipeline<string>()
    .WithStorage<string, string, int>(); // Add string->int storage

// Method 2: Using the plugin system
var storagePlugin = new StoragePlugin<string, string, int>();
var pipeline = new ChannelPipeline<string>()
    .UsePlugin(storagePlugin);
```

### Using Storage in Blocks

```csharp
// Storing values
.AddBlock(new BlockOptions<string>
{
    AsyncTransform = async (text, ct) =>
    {
        // Get storage
        var storage = pipeline.GetStorage<string, string, int>();

        // Store a value
        storage.SetValue("key", 42);

        return text;
    }
})

// Retrieving values
.AddBlock(new BlockOptions<string>
{
    AsyncTransform = async (text, ct) =>
    {
        // Get storage
        var storage = pipeline.GetStorage<string, string, int>();

        // Try to get a value
        if (storage.TryGetValue("key", out int value))
        {
            return $"{text} - {value}";
        }

        return text;
    }
})
```

### Using Storage Modifiers

```csharp
// Store processing results
StorageBlockModifiers.StoreResult<string, string>(
    pipeline,
    text => text // Use the text itself as the key
)

// Enhance items with stored data
StorageBlockModifiers.WithStoredData<string, string, int>(
    pipeline,
    (text, storage) =>
    {
        storage.TryGetValue("count", out var count);
        return $"{text} (Count: {count})";
    }
)

// Skip processing for cached items
StorageBlockModifiers.SkipIfStored<string, string, string>(
    pipeline,
    text => text, // Key selector
    cached => cached != null // Skip if in cache
)
```

## Using the Plugin System

The plugin system allows you to extend pipeline functionality with reusable components. The library includes a simple `LoggingPlugin<T>` that logs when a pipeline is created and disposed:

```csharp
var pipeline = new ChannelPipeline<string>()
    .UsePlugin(new LoggingPlugin<string>(logger))
    .UsePlugin(new StoragePlugin<string, string, int>());

// Retrieve a plugin instance
var storage = pipeline.GetPlugin<string, StoragePlugin<string, string, int>>();
storage?.Storage.SetValue("global", 100);
```

## Creating Custom Storage Implementations

You can create custom storage implementations by implementing the `IBlockStorage<TKey, TValue>` interface:

```csharp
public class RedisBlockStorage<TKey, TValue> : IBlockStorage<TKey, TValue>, IAsyncDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _keyPrefix;

    public RedisBlockStorage(IConnectionMultiplexer redis, string keyPrefix)
    {
        _redis = redis;
        _keyPrefix = keyPrefix;
    }

    public bool TryGetValue(TKey key, out TValue? value)
    {
        // Implementation using Redis
    }

    public void SetValue(TKey key, TValue value)
    {
        // Implementation using Redis
    }

    public bool RemoveValue(TKey key)
    {
        // Implementation using Redis
    }

    public void Clear()
    {
        // Implementation using Redis
    }

    public ValueTask DisposeAsync()
    {
        // Cleanup
    }
}
```

Then use it with your pipeline:

```csharp
var redis = ConnectionMultiplexer.Connect("localhost");
var storage = new RedisBlockStorage<string, int>(redis, "pipeline:");

var pipeline = new ChannelPipeline<string>()
    .UsePlugin(new StoragePlugin<string, string, int>(storage));
```

## Full Example with Storage and Plugins

```csharp
// Create a custom plugin for metrics
var metricsPlugin = new MetricsPlugin<string>();

// Create a pipeline with storage and plugins
var pipeline = new ChannelPipeline<string>(preserveOrder: true)
    .UsePlugin(metricsPlugin)
    .UsePlugin(new StoragePlugin<string, string, int>())

    // First block: process and cache results
    .AddBlock(new BlockOptions<string>
    {
        AsyncTransform = async (text, ct) =>
        {
            await Task.Delay(100, ct); // Simulate work
            return text.ToUpper();
        },
        Modifiers = new BlockModifier<string>[]
        {
            // Skip if already processed
            StorageBlockModifiers.SkipIfStored<string, string, string>(
                pipeline,
                text => text,
                cached => cached != null
            ),
            // Store result
            StorageBlockModifiers.StoreResult<string, string>(
                pipeline,
                text => text.ToLower()
            )
        }
    })

    // Second block: use stored data
    .AddBlock(new BlockOptions<string>
    {
        AsyncTransform = async (text, ct) =>
        {
            var storage = pipeline.GetStorage<string, string, int>();
            storage?.SetValue("processed", storage.TryGetValue("processed", out var count) ? count + 1 : 1);
            return text;
        }
    });

// Feed data
await pipeline.WriteAsync("Hello world", CancellationToken.None);
await pipeline.WriteAsync("Hello world", CancellationToken.None); // Will be skipped due to cache

// Complete and read results
await pipeline.CompleteAsync();
var reader = pipeline.Build();
await foreach (var result in reader.ReadAllAsync())
{
    Console.WriteLine(result);
}

// Get metrics from plugin
var metrics = metricsPlugin.GetMetrics();
Console.WriteLine($"Processed items: {metrics.ProcessedCount}");
Console.WriteLine($"Skipped items: {metrics.SkippedCount}");

await pipeline.DisposeAsync();
```
A composable channel-based pipeline for asynchronous data processing with support for parallelism, ordering, and resilience patterns. This library uses System.Threading.Channels as the underlying infrastructure for efficient asynchronous data processing.

## What is ChannelPipeline?

ChannelPipeline is a flexible framework for building data processing pipelines that:

- **Process data asynchronously** using modern .NET async/await patterns
- **Scale processing** with configurable parallelism for compute-intensive operations
- **Preserve ordering** (optionally) even when processing in parallel
- **Add resilience** with modifiers like retry, timeout, and circuit breakers
- **Handle errors gracefully** at each processing stage
- **Chain transformations** with a fluent API for readable pipeline construction

## When to Use ChannelPipeline

Consider using ChannelPipeline when you need to:

- Process streams of data with multiple transformation steps
- Scale processing with parallel execution while maintaining control
- Add resilience patterns to unreliable operations (API calls, I/O)
- Build ETL processes with complex transformation logic
- Implement the Pipes and Filters architectural pattern
- Create backpressure-aware asynchronous workflows

## Core Components

### Building Blocks
- `ChannelPipeline<T>` – The main pipeline orchestrator that manages execution flow and resource lifecycle
- `BlockOptions<T>` – Configuration for each processing block, including transformation logic and execution settings
- `BlockModifier<T>` – Middleware-like wrappers that enhance blocks with cross-cutting concerns like retries or logging
- `Envelope<T>` – Internal wrapper that tracks sequencing information for maintaining order when needed

### Built-In Modifiers

`BlockModifiers` provides ready-to-use resilience patterns:

- **Retry** – Automatically retry operations that fail with configurable backoff strategies
- **Timeout** – Cancel operations that exceed a specified duration
- **Delay** – Introduce controlled delays between operations (rate limiting)
- **Bulkhead** – Isolate operations to prevent cascading failures by limiting concurrency
- **Fallback** – Specify alternative values when operations fail
- **Throttle** – Enforce minimum intervals between operations

These can be combined by adding them to `BlockOptions.Modifiers` in the desired order.
Modifiers are applied in sequence from first to last in the array.

## Usage Examples

### Basic Pipeline

```csharp
using DomsUtils.Services.Pipeline;

// Create a pipeline that doubles numbers using the simplified overload
var pipeline = new ChannelPipeline<int>()
    .AddBlock(async (value, ct) => value * 2);

// Feed data into the pipeline
await pipeline.WriteAsync(5, CancellationToken.None);

// Get a reader for consuming results
ChannelReader<int> results = pipeline.Build();

// Read and process results
await foreach (var result in results.ReadAllAsync())
{
    Console.WriteLine(result); // Outputs: 10
}

// Clean up resources when done
await pipeline.DisposeAsync();
```

### Multi-stage Pipeline with Parallelism

```csharp
using DomsUtils.Services.Pipeline;

var pipeline = new ChannelPipeline<int>(preserveOrder: true)
    // First stage: double the values with 4 parallel workers using the simplified overload
    .AddBlock(async (v, ct) =>
    {
        await Task.Delay(100, ct); // Simulate work
        return v * 2;
    }, parallelism: 4)
    // Second stage: add 10 to each value
    .AddBlock(async (v, ct) => v + 10);

// Process a batch of items
for (int i = 1; i <= 10; i++)
{
    await pipeline.WriteAsync(i, CancellationToken.None);
}

// Complete the pipeline (no more inputs)
await pipeline.CompleteAsync();

// Read all results in order (preserveOrder: true ensures original sequence)
var reader = pipeline.Build();
await foreach (var result in reader.ReadAllAsync())
{
    Console.WriteLine(result); // Outputs: 12, 14, 16, 18, 20, 22, 24, 26, 28, 30
}
```

### Adding Resilience with Modifiers

```csharp
using DomsUtils.Services.Pipeline;

var pipeline = new ChannelPipeline<string>()
    .AddBlock(new BlockOptions<string>
    {
        AsyncTransform = async (url, ct) => {
            // Simulate API call that might fail
            using var client = new HttpClient();
            var response = await client.GetStringAsync(url, ct);
            return response;
        },
        Modifiers = new BlockModifier<string>[] {
            // Apply timeout of 5 seconds
            BlockModifiers.Timeout<string>(TimeSpan.FromSeconds(5)),

            // Retry up to 3 times with exponential backoff
            BlockModifiers.Retry<string>(3),

            // Limit to 2 concurrent calls
            BlockModifiers.Bulkhead<string>(2),

            // Provide fallback when all else fails
            BlockModifiers.Fallback<string>(ex => new ValueTask<string>("API unavailable"))
        }
    });
```

## Advanced Scenarios

### Processing Different Types

You can chain blocks that transform data between different types by creating multiple pipelines and connecting them:

```csharp
// First pipeline: string → int
var p1 = new ChannelPipeline<string>()
    .AddBlock(new BlockOptions<string> {
        AsyncTransform = async (s, ct) => int.Parse(s)
    });

// Second pipeline: int → double
var p2 = new ChannelPipeline<int>()
    .AddBlock(new BlockOptions<int> {
        AsyncTransform = async (i, ct) => i * 1.5
    });

// Connect pipelines
var reader1 = p1.Build();
await foreach (var i in reader1.ReadAllAsync())
{
    await p2.WriteAsync(i, CancellationToken.None);
}
```

### Custom Block Modifiers

You can create custom modifiers for cross-cutting concerns like logging or metrics:

```csharp
public static BlockModifier<T> WithLogging<T>(ILogger logger)
{
    return next => async (env, ct) => {
        logger.LogInformation("Processing item {Index}", env.Index);
        var sw = Stopwatch.StartNew();

        try {
            var result = await next(env, ct);
            logger.LogInformation("Completed item {Index} in {Elapsed}ms", 
                env.Index, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error processing item {Index}", env.Index);
            throw;
        }
    };
}
```

## Performance Considerations

- **Channel Options**: Use `BoundedChannelOptions` to control backpressure and memory usage
- **Parallelism**: Match parallelism to available CPU cores for CPU-bound work
- **Order Preservation**: Disabling `preserveOrder` improves throughput when order doesn't matter
- **Disposal**: Always call `DisposeAsync()` to properly clean up resources

## Full Example

```csharp
using DomsUtils.Services.Pipeline;

var pipeline = new ChannelPipeline<int>(preserveOrder: true)
    .AddBlock(new BlockOptions<int>
    {
        AsyncTransform = async (v, ct) => v * 2,
        Parallelism = 2,
        Modifiers = new BlockModifier<int>[]
        {
            BlockModifiers.Delay<int>(TimeSpan.FromMilliseconds(100)),
            BlockModifiers.Retry<int>(2)
        }
    });

await pipeline.WriteAsync(5, CancellationToken.None);
await pipeline.DisposeAsync();
```
