# ChannelPipeline

A composable channel-based pipeline for asynchronous data processing. Blocks can be chained to transform incoming items with optional parallelism and error handling.

## Building Blocks
- `ChannelPipeline<T>` – orchestrates processing blocks.
- `BlockOptions<T>` – configure transformations and parallelism.
- `BlockModifier<T>` – delegate to wrap transformations with extra behavior (e.g. logging).
- `Envelope<T>` – wrapper that tracks item ordering when preserving order.

## Example
```csharp
using DomsUtils.Services.Pipeline;

var pipeline = new ChannelPipeline<int>(preserveOrder: true)
    .AddBlock(new BlockOptions<int>
    {
        AsyncTransform = async (v, ct) => v * 2,
        Parallelism = 2
    });

await pipeline.WriteAsync(5, CancellationToken.None);
await pipeline.DisposeAsync();
```
