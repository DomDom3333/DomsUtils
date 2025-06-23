using DomsUtils.Services.Pipeline;
using DomsUtils.Services.Pipeline.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using DomsUtils.Services.Pipeline.Plugins.Core;

namespace DomsUtils.Tests.Services.Pipeline;

[TestClass]
public class PluginSystemTest
{
    private class DummyPlugin<T> : IPipelinePlugin<T>
    {
        public bool Attached { get; private set; }
        public bool Disposed { get; private set; }
        public string Name => "Dummy";

        public void OnAttach(ChannelPipeline<T> pipeline) => Attached = true;
        public void OnDispose(ChannelPipeline<T> pipeline) => Disposed = true;
    }

    [TestMethod]
    public async Task UsePlugin_AttachAndDispose_TriggersCallbacks()
    {
        var plugin = new DummyPlugin<int>();

        await using var pipeline = new ChannelPipeline<int>();
        pipeline.UsePlugin(plugin);

        Assert.IsTrue(plugin.Attached);
        Assert.AreSame(plugin, pipeline.GetPlugin<int, DummyPlugin<int>>());

        await pipeline.DisposeAsync();
        Assert.IsTrue(plugin.Disposed);
    }
}

