using DomsUtils.Services.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DomsUtils.Tests.Services.Pipeline;

[TestClass]
public class BlockModifiersTest
{
    [TestMethod]
    public async Task RetryModifier_RetriesUntilSuccess()
    {
        int attempts = 0;
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = (v, ct) =>
            {
                attempts++;
                if (attempts < 3) throw new InvalidOperationException("boom");
                return ValueTask.FromResult(v);
            },
            Modifiers = new[] { BlockModifiers.Retry<int>(3) }
        });

        var reader = pipeline.Build();
        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.CompleteAsync();

        var enumerator = reader.ReadAllAsync().GetAsyncEnumerator();
        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual(1, enumerator.Current);
        await enumerator.DisposeAsync();
        Assert.AreEqual(3, attempts);
    }

    [TestMethod]
    public async Task TimeoutModifier_ThrowsOnLongRunning()
    {
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = async (v, ct) => { await Task.Delay(200, ct); return v; },
            Modifiers = new[] { BlockModifiers.Timeout<int>(TimeSpan.FromMilliseconds(50)) }
        });

        var reader = pipeline.Build();
        await pipeline.WriteAsync(1, CancellationToken.None);
        await Assert.ThrowsExceptionAsync<TimeoutException>(async () => await pipeline.CompleteAsync());
    }

    [TestMethod]
    public async Task DelayModifier_WaitsBeforeExecution()
    {
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = (v, ct) => ValueTask.FromResult(v),
            Modifiers = new[] { BlockModifiers.Delay<int>(TimeSpan.FromMilliseconds(100)) }
        });
        var reader = pipeline.Build();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.CompleteAsync();
        await foreach (var _ in reader.ReadAllAsync()) { }
        Assert.IsTrue(sw.ElapsedMilliseconds >= 50);
    }

    [TestMethod]
    public async Task BulkheadModifier_LimitsConcurrency()
    {
        await using var pipeline = new ChannelPipeline<int>();
        int running = 0;
        int observedMax = 0;

        pipeline.AddBlock(new BlockOptions<int>
        {
            Parallelism = 3,
            AsyncTransform = async (v, ct) =>
            {
                Interlocked.Increment(ref running);
                observedMax = Math.Max(observedMax, Volatile.Read(ref running));
                await Task.Delay(50, ct);
                Interlocked.Decrement(ref running);
                return v;
            },
            Modifiers = new[] { BlockModifiers.Bulkhead<int>(1) }
        });

        var reader = pipeline.Build();
        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.WriteAsync(2, CancellationToken.None);
        await pipeline.WriteAsync(3, CancellationToken.None);
        await pipeline.CompleteAsync();
        await foreach (var _ in reader.ReadAllAsync()) { }

        Assert.AreEqual(1, observedMax);
    }

    [TestMethod]
    public async Task FallbackModifier_ReplacesErrorValue()
    {
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = (v, ct) => throw new InvalidOperationException(),
            Modifiers = new[] { BlockModifiers.Fallback<int>(_ => ValueTask.FromResult(42)) }
        });

        var reader = pipeline.Build();
        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.CompleteAsync();
        var enumerator = reader.ReadAllAsync().GetAsyncEnumerator();
        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual(42, enumerator.Current);
        await enumerator.DisposeAsync();
    }

    [TestMethod]
    public async Task ThrottleModifier_EnforcesInterval()
    {
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = (v, ct) => ValueTask.FromResult(v),
            Modifiers = new[] { BlockModifiers.Throttle<int>(TimeSpan.FromMilliseconds(50)) }
        });
        var reader = pipeline.Build();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.WriteAsync(2, CancellationToken.None);
        await pipeline.CompleteAsync();
        await foreach (var _ in reader.ReadAllAsync()) { }
        Assert.IsTrue(sw.ElapsedMilliseconds >= 30);
    }

    [TestMethod]
    public async Task Modifiers_CombineSequentially()
    {
        await using var pipeline = new ChannelPipeline<int>();
        int attempts = 0;
        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = (v, ct) =>
            {
                attempts++;
                throw new InvalidOperationException();
            },
            Modifiers = new BlockModifier<int>[]
            {
                BlockModifiers.Retry<int>(2),
                BlockModifiers.Fallback<int>(_ => ValueTask.FromResult(99))
            }
        });

        var reader = pipeline.Build();
        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.CompleteAsync();

        var enumerator = reader.ReadAllAsync().GetAsyncEnumerator();
        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual(99, enumerator.Current);
        await enumerator.DisposeAsync();
        Assert.AreEqual(3, attempts);
    }
}
