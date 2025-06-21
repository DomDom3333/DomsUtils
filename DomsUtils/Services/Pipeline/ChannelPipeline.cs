using System.Threading.Channels;

namespace DomsUtils.Services.Pipeline;

/// <summary>
/// A chainable pipeline for processing data streams using System.Threading.Channels.
/// It provides support for parallel execution, optional ordering of output, error handling,
/// and task observation, allowing scalable and efficient processing of asynchronous data flows.
/// </summary>
public class ChannelPipeline<T> : IAsyncDisposable
{
    /// <summary>
    /// Indicates whether the pipeline should preserve the order of processed items
    /// such that the output matches the input order.
    /// </summary>
    private readonly bool _preserveOrder;

    /// <summary>
    /// Maximum buffer size to store out-of-order items when reordering
    /// pipeline output. If the buffer exceeds this size, an error is triggered.
    /// </summary>
    private readonly int _reorderMaxBufferSize;

    /// <summary>
    /// Tracks the number of input items fed into the pipeline.
    /// </summary
    private long _inputCounter;

    /// <summary>
    /// Holds a list of readers that represent the active processing channels in the pipeline.
    /// </summary>
    private List<ChannelReader<Envelope<T>>> _readers;

    /// <summary>
    /// Tracks the current level of parallelism for processing blocks in the pipeline.
    /// Updated dynamically when adding new blocks or modifying the pipeline's structure.
    /// </summary>
    private int _currentParallelism;

    /// <summary>
    /// A collection of tasks used internally to track asynchronous operations within the pipeline.
    /// This allows proper monitoring, exception handling, and orderly disposal of resources.
    /// </summary>
    private readonly List<Task> _tasks = new();

    /// <summary>
    /// Represents a collection of channels used within the pipeline for data processing.
    /// </summary>
    private readonly List<Channel<Envelope<T>>> _channels = new();

    /// <summary>
    /// A <see cref="CancellationTokenSource"/> used to signal the cancellation of ongoing operations,
    /// particularly for the completion of pipelines and cleanup of associated tasks.
    /// </summary>
    private readonly CancellationTokenSource _completionCts = new();

    /// <summary>
    /// Represents the input channel of the pipeline, which serves as the entry point for data being processed.
    /// </summary>
    private Channel<Envelope<T>> _inputChannel;

    /// <summary>
    /// Indicates whether the pipeline has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Asynchronously writes an item to the input channel of the pipeline.
    /// </summary>
    /// <param name="item">The item to write into the pipeline.</param>
    /// <param name="ct">A cancellation token to observe cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous write operation.</returns>
    /// <remarks>
    /// This method feeds data into the pipeline. If the pipeline is disposed, an exception will be thrown.
    /// It ensures that the provided data is correctly enqueued for processing.
    /// </remarks>
    public Func<T, CancellationToken, ValueTask> WriteAsync { get; }

    /// <summary>
    /// Initializes a new pipeline.
    /// </summary>
    /// <param name="preserveOrder">Reorder output to match input order if true.</param>
    /// <param name="reorderMaxBufferSize">Max out-of-order items to buffer before error.</param>
    public ChannelPipeline(bool preserveOrder = false, int reorderMaxBufferSize = 1000)
    {
        _preserveOrder = preserveOrder;
        _reorderMaxBufferSize = reorderMaxBufferSize;
        _currentParallelism = 1;

        _inputChannel = CreateAndTrackChannel();
        _readers = new List<ChannelReader<Envelope<T>>> { _inputChannel.Reader };
        
        WriteAsync = async (item, ct) =>
        {
            ThrowIfDisposed();
            var idx = Interlocked.Increment(ref _inputCounter);
            await _inputChannel.Writer.WriteAsync(new Envelope<T>(idx, item), ct);
        };
    }

    /// <summary>
    /// Adds a processing block to the pipeline using the specified block options.
    /// </summary>
    /// <param name="options">
    /// The options that configure the processing block, including the transformation
    /// to apply, modifiers, parallelism, channel behavior, cancellation, and error handling.
    /// </param>
    /// <returns>
    /// The pipeline instance with the added processing block for further chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided options parameter is null.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the pipeline instance has been disposed.
    /// </exception>
    public ChannelPipeline<T> AddBlock(BlockOptions<T> options)
    {
        ThrowIfDisposed();
        if (options == null) throw new ArgumentNullException(nameof(options));

        // wrap transform for envelopes
        var transform = options.GetEnvelopeTransform();
        if (options.Modifiers != null)
            foreach (var mod in options.Modifiers)
                transform = mod(transform);

        return AddEnvelopeBlock(transform,
            options.Parallelism,
            options.ChannelOptions,
            options.CancellationToken,
            options.OnError);
    }

    /// <summary>
    /// Adds a processing block to the pipeline that operates on envelopes of data with optional transformations and parallelism.
    /// </summary>
    /// <param name="transform">The transformation function to apply to each envelope.</param>
    /// <param name="parallelism">The degree of parallelism to use for this block.</param>
    /// <param name="channelOptions">The options for configuring the underlying bounded channel, or null for default configuration.</param>
    /// <param name="cancellationToken">A cancellation token to observe while performing operations.</param>
    /// <param name="onError">An optional action to invoke on any exception encountered during processing.</param>
    /// <returns>Returns the updated pipeline instance, allowing further chaining of processing blocks.</returns>
    private ChannelPipeline<T> AddEnvelopeBlock(
        Func<Envelope<T>, CancellationToken, ValueTask<Envelope<T>>> transform,
        int parallelism,
        BoundedChannelOptions? channelOptions,
        CancellationToken cancellationToken,
        Action<Exception>? onError)
    {
        int newFan = CalculateNewFan(parallelism);
        var nextChans = Enumerable.Range(0, newFan)
            .Select(_ => CreateAndTrackChannel(channelOptions))
            .ToArray();

        // fan-out from single reader
        if (_currentParallelism == 1 && newFan > 1)
        {
            var src = _readers[0];
            foreach (var ch in nextChans)
                TrackTask(FanOut(src, ch.Writer, cancellationToken, onError));
        }

        // processors
        for (int i = 0; i < newFan; i++)
        {
            var rdr = _readers[_currentParallelism > 1 ? i % _currentParallelism : 0];
            var wr  = nextChans[i].Writer;
            TrackTask(Process(rdr, wr, transform, cancellationToken, onError));
        }

        // merge if collapsing
        if (_currentParallelism > 1 && newFan == 1)
        {
            var merged = CreateAndTrackChannel();
            foreach (var rdr in _readers)
                TrackTask(FanOut(rdr, merged.Writer, cancellationToken, onError));
            nextChans[0] = merged;
        }

        _readers = nextChans.Select(c => c.Reader).ToList();
        _currentParallelism = newFan;
        return this;
    }

    /// <summary>
    /// Creates a new channel, optionally bounded, and tracks it within the pipeline.
    /// </summary>
    /// <param name="options">Channel bounds and behavior options. If null, creates an unbounded channel.</param>
    /// <returns>A new instance of a tracked channel.</returns>
    private Channel<Envelope<T>> CreateAndTrackChannel(BoundedChannelOptions? options = null)
    {
        var channel = options != null 
            ? Channel.CreateBounded<Envelope<T>>(options)
            : Channel.CreateUnbounded<Envelope<T>>();
        
        lock (_channels)
        {
            _channels.Add(channel);
        }
        return channel;
    }

    /// <summary>
    /// Routes items from a source channel reader to a target channel writer.
    /// </summary>
    /// <param name="reader">The channel reader from which items are read.</param>
    /// <param name="writer">The channel writer to which items are written.</param>
    /// <param name="ct">The cancellation token used to propagate notification of cancellation.</param>
    /// <param name="onError">An optional action invoked when an exception occurs during processing.</param>
    /// <returns>A task representing the asynchronous operation of the fan-out process.</returns>
    private async Task FanOut(ChannelReader<Envelope<T>> reader,
        ChannelWriter<Envelope<T>> writer,
        CancellationToken ct,
        Action<Exception>? onError)
    {
        try
        {
            await foreach (var item in reader.ReadAllAsync(ct))
                await writer.WriteAsync(item, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) 
        { 
            // Expected cancellation, don't propagate
        }
        catch (Exception ex)
        {
            if (onError != null) 
                onError(ex);
            else 
                throw;
        }
        finally
        {
            writer.TryComplete();
        }
    }

    /// <summary>
    /// Processes the items read from the specified ChannelReader, transforms them using the provided function,
    /// and writes the transformed results to the specified ChannelWriter. Supports cancellation and error handling.
    /// </summary>
    /// <param name="reader">The source ChannelReader to read items from.</param>
    /// <param name="writer">The target ChannelWriter to write transformed items to.</param>
    /// <param name="transform">A function to transform each item from the reader before writing it to the writer.</param>
    /// <param name="ct">The CancellationToken used to observe cancellation requests.</param>
    /// <param name="onError">An optional action to handle exceptions that occur during processing.</param>
    /// <returns>A Task that represents the asynchronous processing operation.</returns>
    private async Task Process(
        ChannelReader<Envelope<T>> reader,
        ChannelWriter<Envelope<T>> writer,
        Func<Envelope<T>, CancellationToken, ValueTask<Envelope<T>>> transform,
        CancellationToken ct,
        Action<Exception>? onError)
    {
        try
        {
            await foreach (var item in reader.ReadAllAsync(ct))
            {
                try 
                { 
                    var result = await transform(item, ct);
                    await writer.WriteAsync(result, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) 
                { 
                    throw; 
                }
                catch (Exception ex)
                {
                    if (onError != null) 
                    {
                        onError(ex);
                        // Continue processing other items
                    }
                    else 
                    {
                        throw;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) 
        { 
            // Expected cancellation, don't propagate
        }
        catch (Exception ex)
        {
            if (onError != null) 
            {
                onError(ex);
            }
            else 
            {
                throw;
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }

    /// <summary>
    /// Calculates the new level of parallelism (fan-out/fan-in) based on the current and desired levels of parallelism.
    /// </summary>
    /// <param name="parallelism">The desired level of parallelism for the pipeline operations.</param>
    /// <returns>The computed new level of parallelism (fan).</returns>
    private int CalculateNewFan(int parallelism)
        => (_currentParallelism == 1 && parallelism > 1) ? parallelism
            : (_currentParallelism > 1 && parallelism > 1) ? _currentParallelism
            : (_currentParallelism > 1 && parallelism == 1) ? 1
            : 1;

    /// <summary>
    /// Completes the input and all intermediate writers, then awaits all internal tasks to ensure
    /// the pipeline processes all remaining work before shutting down.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of completing the pipeline.</returns>
    public async ValueTask CompleteAsync()
    {
        if (_disposed) return;

        // Complete the input channel
        _inputChannel.Writer.TryComplete();
        
        // Await all background tasks
        await Task.WhenAll(_tasks.ToArray()).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously releases the resources used by the pipeline, ensuring proper cleanup of channels, tasks, and cancellation tokens.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Complete all writers
        foreach (var channel in _channels)
        {
            channel.Writer.TryComplete();
        }

        // Cancel completion token
        _completionCts.Cancel();

        // Wait for all tasks with timeout
        try
        {
            await Task.WhenAll(_tasks).WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException)
        {
            // Log timeout if needed - tasks may still be running
        }

        _completionCts.Dispose();
    }

    /// <summary>
    /// Builds the final ChannelReader, merging all processing stages and optionally reordering
    /// outputs based on the preserveOrder setting of the pipeline.
    /// </summary>
    /// <returns>A ChannelReader that provides the final output of the pipeline.</returns>
    public ChannelReader<T> Build()
    {
        ThrowIfDisposed();

        ChannelReader<Envelope<T>> final = _readers.Count == 1
            ? _readers[0]
            : MergeReaders(_readers);

        if (!_preserveOrder) 
            return ToValueReader(final);

        return Reorder(final);
    }

    /// <summary>
    /// Merges multiple channel readers into a single channel reader, allowing messages
    /// from all source readers to be read from the merged reader.
    /// </summary>
    /// <param name="srcs">The collection of source channel readers to merge.</param>
    /// <returns>A single channel reader that outputs items from all source readers.</returns>
    private ChannelReader<Envelope<T>> MergeReaders(IEnumerable<ChannelReader<Envelope<T>>> srcs)
    {
        var merged = CreateAndTrackChannel();
        foreach (var r in srcs)
            TrackTask(FanOut(r, merged.Writer, CancellationToken.None, null));
        return merged.Reader;
    }

    /// <summary>
    /// Converts a channel reader of envelopes to a channel reader of values,
    /// extracting the underlying value from each envelope.
    /// </summary>
    /// <param name="reader">The channel reader of envelope objects containing the values.</param>
    /// <returns>A channel reader that provides the values extracted from the envelopes.</returns>
    private ChannelReader<T> ToValueReader(ChannelReader<Envelope<T>> reader)
    {
        var outCh = Channel.CreateUnbounded<T>();
        TrackTask(UnwrapAsync(reader, outCh.Writer, _completionCts.Token));
        return outCh.Reader;

        static async Task UnwrapAsync(
            ChannelReader<Envelope<T>> src,
            ChannelWriter<T> dest,
            CancellationToken ct)
        {
            try
            {
                await foreach (var env in src.ReadAllAsync(ct))
                {
                    await dest.WriteAsync(env.Value, ct);
                }
                dest.Complete();
            }
            catch (Exception ex)
            {
                dest.TryComplete(ex);
            }
        }
    }

    /// <summary>
    /// Reorders items from the given reader based on their sequence index, ensuring they are emitted in order.
    /// </summary>
    /// <param name="reader">The channel reader providing the unordered envelopes of items.</param>
    /// <returns>A channel reader that emits items in sequence order.</returns>
    private ChannelReader<T> Reorder(ChannelReader<Envelope<T>> reader)
    {
        var outCh = Channel.CreateUnbounded<T>();
        TrackTask(Task.Run(async () =>
        {
            try
            {
                var buffer = new SortedDictionary<long, T>();
                long next = 1;
                
                await foreach (var env in reader.ReadAllAsync(_completionCts.Token))
                {
                    buffer[env.Index] = env.Value;
                    
                    if (buffer.Count > _reorderMaxBufferSize)
                    {
                        throw new InvalidOperationException(
                            $"Reorder buffer exceeded maximum size of {_reorderMaxBufferSize}");
                    }

                    // Emit all consecutive items starting from 'next'
                    while (buffer.Remove(next, out var val))
                    {
                        await outCh.Writer.WriteAsync(val, _completionCts.Token);
                        next++;
                    }
                }
                
                // Emit any remaining items (handles gaps in sequence)
                foreach (var kvp in buffer.OrderBy(x => x.Key))
                {
                    await outCh.Writer.WriteAsync(kvp.Value, _completionCts.Token);
                }
                
                outCh.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                outCh.Writer.TryComplete(ex);
            }
        }, _completionCts.Token));
        
        return outCh.Reader;
    }

    /// <summary>
    /// Tracks a task to ensure it is observed and prevents tasks from being tracked after disposal.
    /// </summary>
    /// <param name="task">The task to be tracked and monitored for exceptions.</param>
    private void TrackTask(Task task)
    {
        lock (_tasks)
        {
            if (_disposed)
            {
                // Don't track new tasks if disposed
                return;
            }
            _tasks.Add(task);
        }
        
        // Better exception observation
        _ = task.ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                // Log unhandled exceptions or handle them appropriately
                // Consider adding an UnhandledException event
            }
        }, TaskScheduler.Default);
    }

    /// <summary>
    /// Ensures that the current instance of <see cref="ChannelPipeline{T}"/> has not been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the current instance of <see cref="ChannelPipeline{T}"/> has already been disposed.
    /// </exception>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChannelPipeline<T>));
    }
}