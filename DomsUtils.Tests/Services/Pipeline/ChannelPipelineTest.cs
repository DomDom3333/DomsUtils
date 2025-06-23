using DomsUtils.Services.Pipeline;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace DomsUtils.Tests.Services.Pipeline;

[TestClass]
[TestSubject(typeof(ChannelPipeline<>))]
public class ChannelPipelineTest
{
    [TestMethod]
    public async Task Pipeline_WithSingleBlock_ProcessesAllItems()
    {
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = async (v, ct) => { await Task.Yield(); return v * 2; }
        });

        var reader = pipeline.Build();

        for (var i = 1; i <= 5; i++)
            await pipeline.WriteAsync(i, CancellationToken.None);

        await pipeline.CompleteAsync();

        var results = new List<int>();
        await foreach (var item in reader.ReadAllAsync())
            results.Add(item);

        CollectionAssert.AreEquivalent(new[] { 2, 4, 6, 8, 10 }, results);
    }

    [TestMethod]
    public void AddBlock_NullOptions_ThrowsArgumentNullException()
    {
        var pipeline = new ChannelPipeline<int>();
        Assert.ThrowsException<ArgumentNullException>(() => pipeline.AddBlock(null!));
        pipeline.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [TestMethod]
    public async Task Build_WithPreserveOrder_ReordersOutput()
    {
        await using var pipeline = new ChannelPipeline<int>(preserveOrder: true);

        var gates = new TaskCompletionSource<int>[5];
        for (int i = 0; i < gates.Length; i++)
            gates[i] = new TaskCompletionSource<int>();

        pipeline.AddBlock(new BlockOptions<int>
        {
            Parallelism = 5,
            AsyncTransform = async (v, ct) => await gates[v - 1].Task
        });

        var reader = pipeline.Build();

        for (int i = 1; i <= 5; i++)
            await pipeline.WriteAsync(i, CancellationToken.None);

        var readTask = Task.Run(async () =>
        {
            var list = new List<int>();
            await foreach (var item in reader.ReadAllAsync())
                list.Add(item);
            return list;
        });

        for (int i = 4; i >= 0; i--)
            gates[i].SetResult(i + 1);

        await pipeline.CompleteAsync();

        var results = await readTask;
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, results);
    }

    [TestMethod]
    public async Task Build_ReorderBufferExceeded_ThrowsInvalidOperationException()
    {
        await using var pipeline = new ChannelPipeline<int>(preserveOrder: true, reorderMaxBufferSize: 0);

        var gates = new[] { new TaskCompletionSource<int>(), new TaskCompletionSource<int>() };

        pipeline.AddBlock(new BlockOptions<int>
        {
            Parallelism = 2,
            AsyncTransform = async (v, ct) => await gates[v - 1].Task
        });

        var reader = pipeline.Build();

        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.WriteAsync(2, CancellationToken.None);

        var enumerator = reader.ReadAllAsync().GetAsyncEnumerator();
        var readTask = Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            while (await enumerator.MoveNextAsync()) { }
        });

        gates[1].SetResult(2);
        gates[0].SetResult(1);

        await pipeline.CompleteAsync();
        await readTask;
        await enumerator.DisposeAsync();
    }

    [TestMethod]
    public async Task AddBlock_WithModifier_AppliesModifier()
    {
        BlockModifier<int> mod = next => async (env, ct) =>
        {
            var res = await next(env, ct);
            return new Envelope<int>(res.Index, res.Value * 3);
        };

        await using var pipeline = new ChannelPipeline<int>();
        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = (v, ct) => ValueTask.FromResult(v + 1),
            Modifiers = new[] { mod }
        });

        var reader = pipeline.Build();

        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.WriteAsync(2, CancellationToken.None);

        await pipeline.CompleteAsync();

        var results = new List<int>();
        await foreach (var item in reader.ReadAllAsync())
            results.Add(item);

        CollectionAssert.AreEquivalent(new[] { 6, 9 }, results);
    }

    [TestMethod]
    public async Task AddBlock_WithCanceledToken_SkipsProcessing()
    {
        var cts = new CancellationTokenSource();
        await using var pipeline = new ChannelPipeline<int>();

        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = (v, ct) => ValueTask.FromResult(v),
            CancellationToken = cts.Token
        });

        var reader = pipeline.Build();
        cts.Cancel();

        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.CompleteAsync();

        var items = new List<int>();
        await foreach (var item in reader.ReadAllAsync())
            items.Add(item);

        Assert.AreEqual(0, items.Count);
    }

    [TestMethod]
    public async Task TransformThrows_NoErrorHandler_PropagatesDuringComplete()
    {
        var pipeline = new ChannelPipeline<int>();

        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = (v, ct) => throw new InvalidOperationException("boom")
        });

        var reader = pipeline.Build();
        await pipeline.WriteAsync(1, CancellationToken.None);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            async () => await pipeline.CompleteAsync());
    }

    [TestMethod]
    public async Task TransformThrows_WithErrorHandler_ContinuesProcessing()
    {
        var errors = new List<Exception>();
        await using var pipeline = new ChannelPipeline<int>();

        pipeline.AddBlock(new BlockOptions<int>
        {
            AsyncTransform = (v, ct) =>
            {
                if (v == 2) throw new InvalidOperationException("boom");
                return ValueTask.FromResult(v);
            },
            OnError = ex => errors.Add(ex)
        });

        var reader = pipeline.Build();

        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.WriteAsync(2, CancellationToken.None);
        await pipeline.WriteAsync(3, CancellationToken.None);

        await pipeline.CompleteAsync();

        var results = new List<int>();
        await foreach (var item in reader.ReadAllAsync())
            results.Add(item);

        CollectionAssert.AreEquivalent(new[] { 1, 3 }, results);
        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    public async Task AfterDispose_MethodsThrowObjectDisposedException()
    {
        var pipeline = new ChannelPipeline<int>();
        await pipeline.DisposeAsync();

        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(
            () => pipeline.WriteAsync(1, CancellationToken.None).AsTask());

        Assert.ThrowsException<ObjectDisposedException>(() =>
            pipeline.AddBlock(new BlockOptions<int>
            {
                AsyncTransform = (v, ct) => ValueTask.FromResult(v)
            }));

        Assert.ThrowsException<ObjectDisposedException>(() => pipeline.Build());
    }

    [TestMethod]
    public async Task Parallelism_VariesAcrossBlocks_ProcessesAllItems()
    {
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.AddBlock(new BlockOptions<int>
        {
            Parallelism = 3,
            AsyncTransform = async (v, ct) => { await Task.Delay(10, ct); return v * 2; }
        });

        pipeline.AddBlock(new BlockOptions<int>
        {
            Parallelism = 1,
            AsyncTransform = (v, ct) => ValueTask.FromResult(v - 1)
        });

        var reader = pipeline.Build();

        for (var i = 1; i <= 5; i++)
            await pipeline.WriteAsync(i, CancellationToken.None);

        await pipeline.CompleteAsync();

        var results = new List<int>();
        await foreach (var item in reader.ReadAllAsync())
            results.Add(item);

        CollectionAssert.AreEquivalent(new[] {1,3,5,7,9}, results);
    }

    [TestMethod]
    public async Task CompleteAsync_CanBeCalledMultipleTimes()
    {
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.AddBlock(new BlockOptions<int> { AsyncTransform = (v, ct) => ValueTask.FromResult(v) });

        var reader = pipeline.Build();
        await pipeline.WriteAsync(1, CancellationToken.None);

        await pipeline.CompleteAsync();
        await pipeline.CompleteAsync();

        var list = new List<int>();
        await foreach (var item in reader.ReadAllAsync())
            list.Add(item);

        CollectionAssert.AreEqual(new[] {1}, list);
    }
}
