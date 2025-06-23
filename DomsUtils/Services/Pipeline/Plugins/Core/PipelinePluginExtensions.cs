using DomsUtils.Services.Pipeline.Plugins.Core;

namespace DomsUtils.Services.Pipeline.Plugins;

/// <summary>
/// Extension methods for working with pipeline plugins.
/// </summary>
public static class PipelinePluginExtensions
{
    /// <summary>
    /// Attaches a plugin to a pipeline.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    /// <param name="pipeline">The pipeline to attach the plugin to.</param>
    /// <param name="plugin">The plugin to attach.</param>
    /// <returns>The same pipeline instance for method chaining.</returns>
    public static ChannelPipeline<T> UsePlugin<T>(this ChannelPipeline<T> pipeline, IPipelinePlugin<T> plugin)
    {
        // Register the plugin with the registry
        PipelinePluginRegistry.RegisterPlugin(pipeline, plugin);

        // Ensure plugin cleanup when the pipeline is disposed
        PipelineResourceRegistry.RegisterResource(pipeline, new PluginResource<T>());

        // Notify the plugin it's being attached
        plugin.OnAttach(pipeline);

        return pipeline;
    }

    /// <summary>
    /// Gets a plugin of the specified type that is attached to the pipeline.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    /// <typeparam name="TPlugin">The type of plugin to retrieve.</typeparam>
    /// <param name="pipeline">The pipeline to get the plugin from.</param>
    /// <returns>The plugin instance or null if not found.</returns>
    public static TPlugin? GetPlugin<T, TPlugin>(this ChannelPipeline<T> pipeline) where TPlugin : class, IPipelinePlugin<T>
    {
        return PipelinePluginRegistry.GetPlugin<T, TPlugin>(pipeline);
    }
}
