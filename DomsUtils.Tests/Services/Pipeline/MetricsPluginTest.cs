using DomsUtils.Services.Pipeline;
using DomsUtils.Services.Pipeline.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DomsUtils.Tests.Services.Pipeline;

[TestClass]
public class MetricsPluginTest
{
    [TestMethod]
    public async Task MetricsPlugin_TracksSuccessAndTime()
    {
        var metrics = new MetricsPlugin<int>();

        await using var pipeline = new ChannelPipeline<int>();
        pipeline.UsePlugin(metrics)
            .AddBlock(new BlockOptions<int>
            {
                AsyncTransform = async (v, ct) => { await Task.Delay(10, ct); return v * 2; },
                Modifiers = new[] { metrics.TrackMetrics("double") }
            });

        var reader = pipeline.Build();
        await pipeline.WriteAsync(1, CancellationToken.None);
        await pipeline.WriteAsync(2, CancellationToken.None);
        await pipeline.CompleteAsync();

        await foreach (var _ in reader.ReadAllAsync()) { }
        await pipeline.DisposeAsync();

        var m = metrics.GetMetrics();
        Assert.IsTrue(m.TryGetValue("double.success", out var count) && count == 2);
        Assert.IsTrue(m.ContainsKey("double.time.total"));
        Assert.IsTrue(m.ContainsKey("pipeline.lifetime.ms"));
    }
}
