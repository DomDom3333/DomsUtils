# ChannelPipeline

A composable channel-based pipeline for asynchronous data processing.

## Building Blocks

- `ChannelPipeline<T>` – orchestrates processing blocks.
- `BlockOptions<T>` – configure transformations and parallelism.
- `BlockModifier<T>` – delegate to wrap transformations with extra behavior.
- `Envelope<T>` – wrapper that tracks item ordering.

## Built-In Modifiers

`BlockModifiers` provides helpers for common scenarios:

- **Retry** – retry a block with optional backoff.
- **Timeout** – cancel a block if it exceeds a time limit.
- **Delay** – introduce a fixed delay before executing a block.
- **Bulkhead** – limit the number of concurrent executions.
- **Fallback** – return a fallback value when a block fails.
- **Throttle** – enforce a minimum delay between executions.

These can be combined by adding them to `BlockOptions.Modifiers`.
Modifiers are applied in the order they appear in the array.

## Example

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
