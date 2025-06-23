namespace DomsUtils.Services.Pipeline;

/// <summary>
/// Delegate for modifying the transformation behavior of envelope processing blocks
/// (e.g., adding retry logic, backoff strategies, or other middleware functionalities).
/// </summary>
public delegate Func<Envelope<T>, CancellationToken, ValueTask<Envelope<T>>> BlockModifier<T>(Func<Envelope<T>, CancellationToken, ValueTask<Envelope<T>>> next);