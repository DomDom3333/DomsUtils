using DomsUtils.Services.Pipeline.BlockStorage;
using DomsUtils.Services.Pipeline.PipelinePlugin;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace DomsUtils.Services.Pipeline.Examples;

/// <summary>
/// Examples demonstrating how to create and use custom pipeline plugins.
/// </summary>
public static class CustomPluginExample
{
    /// <summary>
    /// A plugin that collects metrics about pipeline execution.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    public class MetricsPlugin<T> : IPipelinePlugin<T>
    {
        private readonly ConcurrentDictionary<string, long> _counters = new();
        private readonly ConcurrentDictionary<string, Stopwatch> _timers = new();

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public string Name => "MetricsPlugin";

        /// <summary>
        /// Creates a block modifier that tracks execution time and counts processed items.
        /// </summary>
        /// <param name="operationName">Name of the operation being measured.</param>
        /// <returns>A block modifier that tracks metrics.</returns>
        public BlockModifier<T> TrackMetrics(string operationName)
        {
            return next => async (env, ct) =>
            {
                // Start timer
                var sw = Stopwatch.StartNew();

                try
                {
                    // Process the item
                    var result = await next(env, ct);

                    // Increment success counter
                    IncrementCounter($"{operationName}.success");

                    return result;
                }
                catch (Exception)
                {
                    // Increment error counter
                    IncrementCounter($"{operationName}.error");
                    throw;
                }
                finally
                {
                    // Record execution time
                    sw.Stop();
                    RecordExecutionTime(operationName, sw.ElapsedMilliseconds);
                }
            };
        }

        /// <summary>
        /// Increments a named counter.
        /// </summary>
        public void IncrementCounter(string name, long amount = 1)
        {
            _counters.AddOrUpdate(name, amount, (_, current) => current + amount);
        }

        /// <summary>
        /// Records execution time for an operation.
        /// </summary>
        public void RecordExecutionTime(string operation, long milliseconds)
        {
            IncrementCounter($"{operation}.time.total", milliseconds);
            IncrementCounter($"{operation}.time.count");
        }

        /// <summary>
        /// Gets all collected metrics.
        /// </summary>
        public IReadOnlyDictionary<string, long> GetMetrics() => _counters;

        /// <summary>
        /// Gets average execution time for an operation.
        /// </summary>
        public double GetAverageExecutionTime(string operation)
        {
            if (_counters.TryGetValue($"{operation}.time.total", out var total) &&
                _counters.TryGetValue($"{operation}.time.count", out var count) &&
                count > 0)
            {
                return (double)total / count;
            }

            return 0;
        }

        /// <summary>
        /// Called when the plugin is attached to a pipeline.
        /// </summary>
        public void OnAttach(ChannelPipeline<T> pipeline)
        {
            // Record pipeline creation time
            _timers["pipeline.lifetime"] = Stopwatch.StartNew();
        }

        /// <summary>
        /// Called when the pipeline is being disposed.
        /// </summary>
        public void OnDispose(ChannelPipeline<T> pipeline)
        {
            // Record total pipeline lifetime
            if (_timers.TryRemove("pipeline.lifetime", out var sw))
            {
                sw.Stop();
                _counters["pipeline.lifetime.ms"] = sw.ElapsedMilliseconds;
            }
        }
    }

    /// <summary>
    /// Demonstrates using a custom metrics plugin with a pipeline.
    /// </summary>
    public static async Task MetricsExample()
    {
        // Create metrics plugin
        var metrics = new MetricsPlugin<string>();

        // Create a pipeline with the plugin and storage
        var pipeline = new ChannelPipeline<string>(preserveOrder: true);
            pipeline.UsePlugin(metrics)
            .UsePlugin(new StoragePlugin<string, string, int>())

            // First block: expensive operation with metrics
            .AddBlock(new BlockOptions<string>
            {
                AsyncTransform = async (text, ct) =>
                {
                    // Simulate varying processing times
                    var delay = text.Length * 10;
                    await Task.Delay(delay, ct);
                    return text.ToUpper();
                },
                Modifiers = new BlockModifier<string>[]
                {
                    // Add metrics tracking
                    metrics.TrackMetrics("uppercase"),

                    // Skip if already processed
                    StorageBlockModifiers.SkipIfStored<string, string, string>(
                        pipeline,
                        text => text,
                        cached => cached != null
                    ),

                    // Store result
                    StorageBlockModifiers.StoreResult<string, string>(
                        pipeline,
                        text => text
                    )
                },
                Parallelism = 2 // Process in parallel
            })

            // Second block: another operation with metrics
            .AddBlock(new BlockOptions<string>
            {
                AsyncTransform = async (text, ct) =>
                {
                    // Count words
                    int wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                    // Store the count
                    var storage = pipeline.GetStorage<string, string, int>();
                    storage?.SetValue(text, wordCount);

                    return $"{text} ({wordCount} words)";
                },
                Modifiers = new BlockModifier<string>[]
                {
                    // Add metrics tracking
                    metrics.TrackMetrics("word-count")
                }
            });

        // Process some items
        var items = new[]
        {
            "Hello world",
            "This is a longer text with more words to process",
            "Hello world", // Duplicate should be skipped
            "Another example text",
            "The quick brown fox jumps over the lazy dog"
        };

        foreach (var item in items)
        {
            metrics.IncrementCounter("items.submitted");
            await pipeline.WriteAsync(item, CancellationToken.None);
        }

        // Complete the pipeline
        await pipeline.CompleteAsync();

        // Read and display results
        var reader = pipeline.Build();
        await foreach (var result in reader.ReadAllAsync())
        {
            Console.WriteLine(result);
        }

        // Display metrics
        Console.WriteLine("\nPipeline Metrics:");
        Console.WriteLine($"Items submitted: {metrics.GetMetrics()["items.submitted"]}");
        Console.WriteLine($"Uppercase operations: {metrics.GetMetrics()["uppercase.success"]}");
        Console.WriteLine($"Word count operations: {metrics.GetMetrics()["word-count.success"]}");
        Console.WriteLine($"Avg uppercase time: {metrics.GetAverageExecutionTime("uppercase"):F2} ms");
        Console.WriteLine($"Avg word count time: {metrics.GetAverageExecutionTime("word-count"):F2} ms");
        Console.WriteLine($"Pipeline lifetime: {metrics.GetMetrics()["pipeline.lifetime.ms"]} ms");

        // Clean up
        await pipeline.DisposeAsync();
    }

    /// <summary>
    /// Plugin that provides a shared value factory for all blocks in a pipeline.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    /// <typeparam name="TKey">The type of keys used for lookups.</typeparam>
    /// <typeparam name="TValue">The type of values created by the factory.</typeparam>
    public class ValueFactoryPlugin<T, TKey, TValue> : IPipelinePlugin<T> where TKey : notnull
    {
        private readonly Func<TKey, CancellationToken, Task<TValue>> _factory;
        private readonly IBlockStorage<TKey, TValue> _cache;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public string Name => $"ValueFactory<{typeof(TKey).Name},{typeof(TValue).Name}>";

        /// <summary>
        /// Creates a new value factory plugin.
        /// </summary>
        /// <param name="factory">Async function that creates values on demand.</param>
        /// <param name="cache">Optional cache to store created values.</param>
        public ValueFactoryPlugin(
            Func<TKey, CancellationToken, Task<TValue>> factory,
            IBlockStorage<TKey, TValue>? cache = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _cache = cache ?? new InMemoryBlockStorage<TKey, TValue>();
        }

        /// <summary>
        /// Gets a value for the specified key, creating it if it doesn't exist.
        /// </summary>
        public async Task<TValue> GetOrCreateValueAsync(TKey key, CancellationToken ct)
        {
            // Check if already in cache
            if (_cache.TryGetValue(key, out var value))
                return value!;

            // Prevent concurrent creation of the same value
            await _semaphore.WaitAsync(ct);
            try
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(key, out value))
                    return value!;

                // Create value and cache it
                value = await _factory(key, ct);
                _cache.SetValue(key, value!);
                return value!;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Creates a block modifier that uses the value factory.
        /// </summary>
        /// <param name="keySelector">Function to derive a key from the processed item.</param>
        /// <param name="transformer">Function that uses the created value to transform the item.</param>
        public BlockModifier<T> WithCreatedValue(
            Func<T, TKey> keySelector,
            Func<T, TValue, T> transformer)
        {
            return next => async (env, ct) =>
            {
                // Get or create the value
                var key = keySelector(env.Value);
                var value = await GetOrCreateValueAsync(key, ct);

                // Transform the item with the value
                var transformedValue = transformer(env.Value, value);
                var transformedEnv = new Envelope<T>(env.Index, transformedValue);

                // Continue processing
                return await next(transformedEnv, ct);
            };
        }

        /// <summary>
        /// Called when the plugin is attached to a pipeline.
        /// </summary>
        public void OnAttach(ChannelPipeline<T> pipeline)
        {
            // Register storage if needed
            if (_cache is InMemoryBlockStorage<TKey, TValue>)
            {
                PipelineStorageRegistry.RegisterStorage(pipeline, _cache);
            }
        }

        /// <summary>
        /// Called when the pipeline is being disposed.
        /// </summary>
        public void OnDispose(ChannelPipeline<T> pipeline)
        {
            // Dispose semaphore
            _semaphore.Dispose();

            // Dispose cache if it's disposable
            if (_cache is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Demonstrates using a value factory plugin with a pipeline.
    /// </summary>
    public static async Task ValueFactoryExample()
    {
        // Create a "slow" value factory that simulates loading data from a database
        var valueFactory = new ValueFactoryPlugin<string, string, Dictionary<string, object>>(
            async (key, ct) =>
            {
                // Simulate database lookup
                Console.WriteLine($"Loading data for key: {key}");
                await Task.Delay(500, ct); // Simulate network delay

                // Return mock data
                return new Dictionary<string, object>
                {
                    ["id"] = key,
                    ["timestamp"] = DateTime.UtcNow,
                    ["count"] = key.Length,
                    ["hash"] = key.GetHashCode()
                };
            });

        // Create pipeline with the factory plugin
        var pipeline = new ChannelPipeline<string>()
            .UsePlugin(valueFactory)

            // First block: enrich items with data from the factory
            .AddBlock(new BlockOptions<string>
            {
                AsyncTransform = async (text, ct) =>
                {
                    return text.ToUpper();
                },
                Modifiers = new BlockModifier<string>[]
                {
                    // Use the value factory to enrich the item
                    valueFactory.WithCreatedValue(
                        text => text.Split(' ')[0], // Use first word as key
                        (text, data) =>
                        {
                            // Append some data from the factory
                            return $"{text} [ID:{data["id"]}, Hash:{data["hash"]}]";
                        }
                    )
                },
                Parallelism = 3 // Process in parallel to show factory concurrency handling
            });

        // Process some items
        var items = new[]
        {
            "Hello world",
            "Hello universe", // Same key as above, should reuse cached value
            "Goodbye world",
            "Hello again", // Same key again
            "Testing factory"
        };

        foreach (var item in items)
        {
            await pipeline.WriteAsync(item, CancellationToken.None);
        }

        // Complete and read results
        await pipeline.CompleteAsync();
        var reader = pipeline.Build();

        Console.WriteLine("\nResults with enriched data:");
        await foreach (var result in reader.ReadAllAsync())
        {
            Console.WriteLine(result);
        }

        await pipeline.DisposeAsync();
    }
}
