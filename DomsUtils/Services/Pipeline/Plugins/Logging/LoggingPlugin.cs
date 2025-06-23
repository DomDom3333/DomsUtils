using DomsUtils.Services.Pipeline.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace DomsUtils.Services.Pipeline.Plugins;

/// <summary>
/// Simple plugin that logs when a pipeline is attached and disposed.
/// </summary>
/// <typeparam name="T">The type of items processed by the pipeline.</typeparam>
public class LoggingPlugin<T> : IPipelinePlugin<T>
{
    private readonly ILogger _logger;
    public string Name => "LoggingPlugin";

    /// <summary>
    /// Initializes the plugin with the provided logger.
    /// </summary>
    public LoggingPlugin(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void OnAttach(ChannelPipeline<T> pipeline)
    {
        _logger.LogInformation("Pipeline created");
    }

    /// <inheritdoc />
    public void OnDispose(ChannelPipeline<T> pipeline)
    {
        _logger.LogInformation("Pipeline disposed");
    }
}

