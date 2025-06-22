using System.Collections.Concurrent;
using System.Text.Json;
using DomsUtils.Services.Pipeline.BlockStorage;
using DomsUtils.Services.Pipeline.PipelinePlugin;
using DomsUtils.Services.Pipeline.BlockStorage;

namespace DomsUtils.Services.Pipeline.Examples;

public static class AdvancedStorageExample
{
    public static async Task RunExample()
    {
        // Create different storage instances using the factory
        var cache = StorageFactory.WithExpiration<string, string>(
            StorageFactory.InMemory<string, string>(),
            TimeSpan.FromMinutes(5));

        var persistentCache = StorageFactory.Layered<string, string>(
            cache, // Fast in-memory cache with expiration
            StorageFactory.InMemory<string, string>() // Persistent storage without expiration
        );

        // Create a pipeline with multiple storage types
        var pipeline = new ChannelPipeline<string>()
            // Add expiring cache storage
            .WithStorage<string, string, string>("cache", cache)

            // Add layered persistent cache
            .WithStorage<string, string, string>("persistent", persistentCache)

            // Add counter storage
            .WithStorage<string, string, int>("counters");

        // Create a processing block with storage modifiers
        pipeline.AddBlock(new BlockOptions<string>
        {
            AsyncTransform = async (text, ct) =>
            {
                await Task.Delay(100, ct); // Simulate work
                return text.ToUpper();
            },
            Modifiers = new BlockModifier<string>[]
            {
                // Skip processing if the item is in the cache
                StorageBlockModifiers.SkipIfStored<string, string, string>(
                    pipeline,
                    text => text,
                    cached => cached != null,
                    "cache"
                ),

                // Store the result in both caches
                StorageBlockModifiers.StoreResult<string, string>(
                    pipeline,
                    text => text.ToLower(),
                    "cache"
                ),

                StorageBlockModifiers.StoreResult<string, string>(
                    pipeline,
                    text => text.ToLower(),
                    "persistent"
                ),

                // Capture any errors
                StorageBlockModifiers.CaptureErrors<string, string>(
                    pipeline,
                    text => text
                )
            }
        });

        // Add a block that updates counters
        pipeline.AddBlock(new BlockOptions<string>
        {
            AsyncTransform = async (text, ct) =>
            {
                // Get the counters storage
                var counters = pipeline.GetStorage<string, int>("counters");
                if (counters != null)
                {
                    // Update the counter for this word
                    counters.TryGetValue(text, out var count);
                    counters.SetValue(text, count + 1);

                    // Update the total counter
                    counters.TryGetValue("total", out var total);
                    counters.SetValue("total", total + 1);
                }

                return text;
            }
        });

        // Add a block that uses the StoreValue modifier to store metadata
        pipeline.AddBlock(new BlockOptions<string>
        {
            AsyncTransform = async (text, ct) => text,
            Modifiers = new BlockModifier<string>[]
            {
                // Store the length of each word
                StorageBlockModifiers.StoreValue<string, string, int>(
                    pipeline,
                    text => $"{text}_length",
                    text => text.Length,
                    "counters"
                )
            }
        });

        // Process some data
        await pipeline.WriteAsync("Hello", CancellationToken.None);
        await pipeline.WriteAsync("World", CancellationToken.None);
        await pipeline.WriteAsync("Hello", CancellationToken.None); // Should be cached

        // Complete and read results
        await pipeline.CompleteAsync();
        var reader = pipeline.Build();
        await foreach (var result in reader.ReadAllAsync())
        {
            Console.WriteLine(result);
        }

        // Access the counters storage to show final counts
        var finalCounters = pipeline.GetStorage<string, int>("counters");
        if (finalCounters != null)
        {
            finalCounters.TryGetValue("total", out var total);
            Console.WriteLine($"Total items processed: {total}");

            finalCounters.TryGetValue("HELLO", out var helloCount);
            Console.WriteLine($"Hello count: {helloCount}");

            finalCounters.TryGetValue("HELLO_length", out var helloLength);
            Console.WriteLine($"Hello length: {helloLength}");
        }

        // Clean up
        await pipeline.DisposeAsync();

        // Clean up the expiring cache separately since it implements IAsyncDisposable
        if (cache is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
