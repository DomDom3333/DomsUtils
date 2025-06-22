# ChannelPipeline

A composable channel-based pipeline for asynchronous data processing.

## Building Blocks

- `ChannelPipeline<T>` – orchestrates processing blocks.
- `BlockOptions<T>` – configure transformations and parallelism.
- `BlockModifier<T>` – delegate to wrap transformations with extra behavior.
- `Envelope<T>` – wrapper that tracks item ordering.

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

