using DomsUtils.Services.Pipeline;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DomsUtils.Tests.Services.Pipeline;

[TestClass]
[TestSubject(typeof(Pipeline<>))]
public class PipelineBuilderTest
{
    [TestMethod]
    public async Task RunAsync_BranchingPipeline_ReturnsExpectedResults()
    {
        var results = await Pipeline<int>.Create()
            .Pipe(v => new ValueTask<int>(v + 1))
            .Split(
                b => b.Pipe(v => new ValueTask<int>(v * 2))
                      .Pipe(v => new ValueTask<int>(v - 1)),
                b => b.Pipe(v => new ValueTask<int>(v * v))
            )
            .Merge()
            .Pipe(v => new ValueTask<int>(v % 100))
            .RunAsync(Enumerable.Range(1, 5));

        var expected = new[] { 3,5,7,9,11,4,9,16,25,36 };
        CollectionAssert.AreEquivalent(expected, results);
    }

    [TestMethod]
    public void Pipe_BetweenSplitAndMerge_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            Pipeline<int>.Create()
                .Split(b => b)
                .Pipe(v => new ValueTask<int>(v));
        });
    }

    [TestMethod]
    public void Merge_WithoutSplit_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            Pipeline<int>.Create().Merge());
    }
}
