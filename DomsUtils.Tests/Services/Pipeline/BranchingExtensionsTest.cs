using DomsUtils.Services.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DomsUtils.Tests.Services.Pipeline;

[TestClass]
public class BranchingExtensionsTest
{
    [TestMethod]
    public async Task BranchIf_SelectsCorrectBranch()
    {
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.BranchIf(
            v => v % 2 == 0,
            new BlockOptions<int> { AsyncTransform = (v, ct) => ValueTask.FromResult(v * 2) },
            new BlockOptions<int> { AsyncTransform = (v, ct) => ValueTask.FromResult(v + 1) }
        );

        var reader = pipeline.Build();

        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.WriteAsync(2, CancellationToken.None);
        await pipeline.WriteAsync(3, CancellationToken.None);
        await pipeline.WriteAsync(4, CancellationToken.None);
        await pipeline.CompleteAsync();

        var results = new List<int>();
        await foreach (var item in reader.ReadAllAsync())
            results.Add(item);

        CollectionAssert.AreEqual(new[] { 2, 4, 4, 8 }, results);
    }

    [TestMethod]
    public async Task BranchIf_NoFalseBranch_PassThrough()
    {
        await using var pipeline = new ChannelPipeline<int>();
        pipeline.BranchIf(
            v => v > 2,
            new BlockOptions<int> { AsyncTransform = (v, ct) => ValueTask.FromResult(v * 10) }
        );

        var reader = pipeline.Build();

        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.WriteAsync(2, CancellationToken.None);
        await pipeline.WriteAsync(3, CancellationToken.None);
        await pipeline.CompleteAsync();

        var results = new List<int>();
        await foreach (var item in reader.ReadAllAsync())
            results.Add(item);

        CollectionAssert.AreEqual(new[] { 1, 2, 30 }, results);
    }
}
