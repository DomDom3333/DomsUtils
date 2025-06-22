using DomsUtils.Services.Pipeline.BlockStorage;

namespace DomsUtils.Services.Pipeline.Examples;

public static class NamedStorageExample
{
    public static async Task RunExample()
    {
        // Create a pipeline with multiple named storage instances
        var pipeline = new ChannelPipeline<string>()
            // Add a default string->int storage
            .WithStorage<string, string, int>()

            // Add a named "cache" storage for string->string
            .WithStorage<string, string, string>("cache")

            // Add a named "metadata" storage for string->Dictionary<string,object>
            .WithStorage<string, string, Dictionary<string, object>>("metadata");

        // Create a block that uses multiple storage instances
        pipeline.AddBlock(new BlockOptions<string>
        {
            AsyncTransform = async (text, ct) =>
            {
                // Get the default storage
                var counterStorage = pipeline.GetStorage<string, int>();
                if (counterStorage != null)
                {
                    counterStorage.TryGetValue("counter", out var count);
                    counterStorage.SetValue("counter", count + 1);
                }

                // Get the cache storage by name
                var cacheStorage = pipeline.GetStorage<string, string>("cache");
                if (cacheStorage != null)
                {
                    // Use the cache
                    if (cacheStorage.TryGetValue(text, out var cached))
                    {
                        return cached;
                    }

                    // Process and cache the result
                    var processed = text.ToUpper();
                    cacheStorage.SetValue(text, processed);
                    return processed;
                }

                return text.ToUpper();
            },
            Modifiers = new BlockModifier<string>[]
            {
                // Use the named cache storage in a modifier
                StorageBlockModifiers.SkipIfStored<string, string, string>(
                    pipeline,
                    text => text,
                    cached => cached != null,
                    "cache" // Explicitly specify the storage name
                )
            }
        });

        // Add another block that uses the metadata storage
        pipeline.AddBlock(new BlockOptions<string>
        {
            AsyncTransform = async (text, ct) =>
            {
                // Get the metadata storage by name
                var metadataStorage = pipeline.GetStorage<string, Dictionary<string, object>>("metadata");
                if (metadataStorage != null)
                {
                    if (!metadataStorage.TryGetValue("stats", out var statsObj))
                    {
                        statsObj = new Dictionary<string, object>();
                        metadataStorage.SetValue("stats", statsObj);
                    }

                    var stats = (Dictionary<string, object>)statsObj!;
                    if (!stats.TryGetValue("totalLength", out var totalLengthObj))
                    {
                        stats["totalLength"] = text.Length;
                    }
                    else
                    {
                        stats["totalLength"] = (int)totalLengthObj + text.Length;
                    }
                }

                return text;
            }
        });

        // Process some data
        await pipeline.WriteAsync("Hello", CancellationToken.None);
        await pipeline.WriteAsync("World", CancellationToken.None);

        // Complete and read results
        await pipeline.CompleteAsync();
        var reader = pipeline.Build();
        await foreach (var result in reader.ReadAllAsync())
        {
            Console.WriteLine(result);
        }

        // Clean up
        await pipeline.DisposeAsync();
    }
}
