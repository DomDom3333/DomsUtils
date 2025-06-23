using System.Threading.Channels;

namespace DomsUtils.Services.Pipeline;

/// <summary>
/// Represents configuration options for a pipeline block, defining its behavior.
/// </summary>
public class BlockOptions<T>
{
    /// <summary>
    /// Defines an asynchronous transformation function applied to items in the pipeline block.
    /// </summary>
    /// <remarks>
    /// This function processes each pipeline item asynchronously. It takes an input value of type <typeparamref name="T"/>
    /// and a CancellationToken, and returns a ValueTask that yields the transformed result.
    /// </remarks>
    private Func<T, CancellationToken, ValueTask<T>>? _asyncTransform;

    /// <summary>
    /// An asynchronous transformation function applied to input items in the pipeline.
    /// This property is required and defines the core logic for processing each item.
    /// </summary>
    public required Func<T, CancellationToken, ValueTask<T>> AsyncTransform
    {
        get => _asyncTransform ?? throw new InvalidOperationException("AsyncTransform is required");
        init => _asyncTransform = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// A collection of optional block modifiers, such as retry or backoff mechanisms,
    /// which can wrap and alter the behavior of the envelope transformation.
    /// </summary>
    public IReadOnlyList<BlockModifier<T>>? Modifiers { get; init; }

    /// <summary>
    /// Determines the level of concurrency for processing items within the pipeline block.
    /// Defaults to a value of 1 if no other value is specified. Must be a positive integer.
    /// </summary>
    private int _parallelism = 1;

    /// <summary>
    /// Specifies the level of parallelism for the processing block. Determines the maximum number of tasks
    /// that can concurrently execute the transformation logic within the block.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is set to a non-positive integer. Parallelism must be greater than zero.
    /// </exception>
    public int Parallelism
    {
        get => _parallelism;
        init => _parallelism = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Parallelism must be positive");
    }

    /// <summary>
    /// Configuration options for the underlying bounded channel used by the processing block.
    /// </summary>
    public BoundedChannelOptions? ChannelOptions { get; init; }

    /// <summary>
    /// The <see cref="CancellationToken"/> used to signal the cancellation of operations
    /// performed in the pipeline block.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    /// <summary>
    /// Optional delegate to handle exceptions occurring during the pipeline block execution.
    /// </summary>
    /// <remarks>
    /// This action gets invoked with the exception encountered during the processing of the pipeline.
    /// It can be used for logging, error handling, or custom recovery mechanisms. If null,
    /// exceptions will not be explicitly handled within the block and may propagate.
    /// </remarks>
    public Action<Exception>? OnError { get; init; }

    /// <summary>
    /// Retrieves a delegate that applies a transformation to an <see cref="Envelope{T}"/> object.
    /// The transformation includes executing the user-defined asynchronous function specified in the
    /// <see cref="AsyncTransform"/> property of the <see cref="BlockOptions{T}"/> instance.
    /// </summary>
    /// <returns>
    /// A delegate that takes an <see cref="Envelope{T}"/> and a <see cref="CancellationToken"/>,
    /// applies the transformation defined in <see cref="AsyncTransform"/>, and returns a new
    /// <see cref="Envelope{T}"/> containing the transformed value.
    /// </returns>
    internal Func<Envelope<T>, CancellationToken, ValueTask<Envelope<T>>> GetEnvelopeTransform()
        => async (env, ct) => new Envelope<T>(env.Index, await AsyncTransform(env.Value, ct));
}