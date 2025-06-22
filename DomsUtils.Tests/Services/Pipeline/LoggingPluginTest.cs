using DomsUtils.Services.Pipeline;
using DomsUtils.Services.Pipeline.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomsUtils.Tests.Services.Pipeline;

[TestClass]
public class LoggingPluginTest
{
    private class ListLogger : ILogger
    {
        public List<string> Messages { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    [TestMethod]
    public async Task LoggingPlugin_WritesLifecycleMessages()
    {
        var logger = new ListLogger();
        var plugin = new LoggingPlugin<int>(logger);

        await using var pipeline = new ChannelPipeline<int>();
        pipeline.UsePlugin(plugin);

        await pipeline.DisposeAsync();

        CollectionAssert.AreEqual(new[] { "Pipeline created", "Pipeline disposed" }, logger.Messages);
    }
}
