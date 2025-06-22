using System;
using System.Threading;
using System.Threading.Tasks;

namespace DomsUtils.Services.Pipeline;

/// <summary>
/// Collection of helper factories for creating <see cref="BlockModifier{T}"/> delegates.
/// These modifiers can wrap a block's transformation logic with extra behaviour
/// such as retry policies, timeouts or simple delays.
/// </summary>
public static class BlockModifiers
{
    /// <summary>
    /// Wraps a transformation with retry logic and optional backoff.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries on failure.</param>
    /// <param name="backoffStrategy">Function that returns the delay before each retry based on the attempt number.</param>
    /// <typeparam name="T">The item type processed by the block.</typeparam>
    /// <returns>A modifier that retries the inner block on error.</returns>
    public static BlockModifier<T> Retry<T>(int maxRetries, Func<int, TimeSpan>? backoffStrategy = null)
    {
        if (maxRetries < 0) throw new ArgumentOutOfRangeException(nameof(maxRetries));
        backoffStrategy ??= attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));

        return next => async (env, ct) =>
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    return await next(env, ct).ConfigureAwait(false);
                }
                catch (Exception) when (attempt < maxRetries)
                {
                    await Task.Delay(backoffStrategy(attempt), ct).ConfigureAwait(false);
                    attempt++;
                }
            }
        };
    }

    /// <summary>
    /// Adds a timeout to a block's execution. Throws <see cref="TimeoutException"/> on expiry.
    /// </summary>
    public static BlockModifier<T> Timeout<T>(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

        return next => async (env, ct) =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            try
            {
                return await next(env, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Block timed out after {timeout}");
            }
        };
    }

    /// <summary>
    /// Delays execution of a block by the specified duration.
    /// </summary>
    public static BlockModifier<T> Delay<T>(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay));

        return next => async (env, ct) =>
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct).ConfigureAwait(false);
            return await next(env, ct).ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Limits concurrent executions of the wrapped block using a semaphore.
    /// </summary>
    /// <param name="maxConcurrency">Maximum number of parallel executions.</param>
    /// <typeparam name="T">The item type processed by the block.</typeparam>
    /// <returns>A modifier restricting concurrency.</returns>
    public static BlockModifier<T> Bulkhead<T>(int maxConcurrency)
    {
        if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
        var sem = new SemaphoreSlim(maxConcurrency);

        return next => async (env, ct) =>
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return await next(env, ct).ConfigureAwait(false);
            }
            finally
            {
                sem.Release();
            }
        };
    }

    /// <summary>
    /// Substitutes a fallback value if the wrapped block throws an exception.
    /// </summary>
    /// <param name="fallbackAsync">Function providing a fallback value based on the error.</param>
    /// <typeparam name="T">The item type processed by the block.</typeparam>
    /// <returns>A modifier that returns a fallback value when an error occurs.</returns>
    public static BlockModifier<T> Fallback<T>(Func<Exception, ValueTask<T>> fallbackAsync)
    {
        ArgumentNullException.ThrowIfNull(fallbackAsync);

        return next => async (env, ct) =>
        {
            try
            {
                return await next(env, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var val = await fallbackAsync(ex).ConfigureAwait(false);
                return new Envelope<T>(env.Index, val);
            }
        };
    }

    /// <summary>
    /// Ensures a minimum interval between consecutive block executions.
    /// </summary>
    /// <param name="interval">Interval to wait between executions.</param>
    /// <typeparam name="T">The item type processed by the block.</typeparam>
    /// <returns>A modifier that throttles execution rate.</returns>
    public static BlockModifier<T> Throttle<T>(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
        var sem = new SemaphoreSlim(1, 1);
        var last = DateTime.UtcNow - interval;

        return next => async (env, ct) =>
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var wait = last + interval - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                    await Task.Delay(wait, ct).ConfigureAwait(false);
                last = DateTime.UtcNow;
            }
            finally
            {
                sem.Release();
            }

            return await next(env, ct).ConfigureAwait(false);
        };
    }
}
