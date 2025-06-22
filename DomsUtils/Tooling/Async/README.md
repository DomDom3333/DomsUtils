# AsyncUtils

Utility helpers for running synchronous and asynchronous delegates.

## Functions

- `RunSync(Action)` and `RunSync(Func<T>)` – execute immediately.
- `RunAsync(Func<Task>)` / `RunAsync(Func<Task<T>>)` – run on the thread pool.
- `FireAndForget(Func<Task>, onError)` – background run with optional error handler.
- `WithTimeout(task, timeout)` – enforce a timeout.
- `RetryAsync(operation, maxRetries)` – retry with exponential backoff.

## Example

```csharp
using DomsUtils.Tooling.Async;

await (() => Task.Delay(100)).RunAsync();

AsyncUtils.FireAndForget(async () =>
{
    await Task.Delay(50);
    Console.WriteLine("done");
});
```

