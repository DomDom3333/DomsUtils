using System;
using System.Threading;
using System.Threading.Tasks;

namespace DomsUtils.Tooling.Async;

/// <summary>
/// Simplified helpers for running sync & async delegates,
/// including fire-and-forget, timeout, and retry utilities.
/// </summary>
public static class AsyncUtils
{
    // Default unhandled exception handler
    private static Action<Exception> _defaultErrorHandler = ex =>
        Console.WriteLine($"Unhandled exception: {ex}");

    /// <summary>
    /// Configure the default error handler used by background operations.
    /// </summary>
    public static void ConfigureErrorHandler(Action<Exception> handler)
    {
        _defaultErrorHandler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Run a synchronous action immediately.
    /// </summary>
    public static void RunSync(this Action action) => action();

    /// <summary>
    /// Run a synchronous function immediately.
    /// </summary>
    public static T RunSync<T>(this Func<T> func) => func();

    /// <summary>
    /// Run an async function on the thread-pool.
    /// </summary>
    public static Task RunAsync(this Func<Task> func)
        => Task.Run(func);

    /// <summary>
    /// Run an async function on the thread-pool and return its result.
    /// </summary>
    public static Task<T> RunAsync<T>(this Func<Task<T>> func)
        => Task.Run<Task<T>>(func).Unwrap();

    /// <summary>
    /// Fire-and-forget an asynchronous operation with optional error handling.
    /// </summary>
    public static void FireAndForget(
        this Func<Task> func,
        Action<Exception>? onError = null,
        CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.Token.ThrowIfCancellationRequested();
                await func().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // canceled
            }
            catch (Exception ex)
            {
                (onError ?? _defaultErrorHandler)(ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Enforce a timeout on a task.
    /// </summary>
    public static async Task<T> WithTimeout<T>(
        this Task<T> task,
        TimeSpan timeout)
    {
        var delayTask = Task.Delay(timeout);
        var completed = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
        if (completed == task)
            return await task.ConfigureAwait(false);
        throw new TimeoutException($"Operation timed out after {timeout}");
    }

    /// <summary>
    /// Retry an async operation with exponential backoff.
    /// </summary>
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? initialDelay = null)
    {
        initialDelay ??= TimeSpan.FromSeconds(1);
        var delay = initialDelay.Value;
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception) when (attempt < maxRetries)
            {
                await Task.Delay(delay).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
    }
}