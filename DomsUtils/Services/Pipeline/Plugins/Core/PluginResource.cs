namespace DomsUtils.Services.Pipeline.Plugins.Core;

/// <summary>
/// Adapts the plugin system to the IPipelineResource interface to handle pipeline lifecycle events.
/// </summary>
/// <remarks>
/// This class allows the plugin system to be registered with the PipelineResourceRegistry
/// and be notified when pipelines are disposed.
/// </remarks>
internal class PluginResource<T> : IPipelineResource
{
    /// <summary>
    /// Called when the associated pipeline is being disposed.
    /// </summary>
    /// <param name="pipeline">The pipeline that is being disposed.</param>
    public void OnPipelineDisposing(object pipeline)
    {
        // Clean up and notify plugins
        if (pipeline is ChannelPipeline<T> typedPipeline)
        {
            PipelinePluginRegistry.CleanupPipeline(typedPipeline);
        }
    }
}
