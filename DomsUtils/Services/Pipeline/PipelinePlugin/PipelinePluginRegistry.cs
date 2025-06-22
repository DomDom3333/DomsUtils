using System.Collections.Concurrent;

namespace DomsUtils.Services.Pipeline.PipelinePlugin;

/// <summary>
/// Registry that manages plugin instances associated with pipelines.
/// </summary>
internal static class PipelinePluginRegistry
{
    // Using object as the value type since we need to store different IPipelinePlugin<T> types
    private static readonly ConcurrentDictionary<object, ConcurrentDictionary<Type, object>> _pluginMap = new();

    /// <summary>
    /// Registers a plugin with a pipeline.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    /// <param name="pipeline">The pipeline to associate the plugin with.</param>
    /// <param name="plugin">The plugin instance to register.</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    internal static bool RegisterPlugin<T>(object pipeline, IPipelinePlugin<T> plugin)
    {
        var pluginType = plugin.GetType();

        var pipelinePlugins = _pluginMap.GetOrAdd(pipeline, _ => new ConcurrentDictionary<Type, object>());
        return pipelinePlugins.TryAdd(pluginType, plugin);
    }

    /// <summary>
    /// Retrieves a plugin of the specified type associated with a pipeline.
    /// </summary>
    /// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
    /// <typeparam name="TPlugin">The type of plugin to retrieve.</typeparam>
    /// <param name="pipeline">The pipeline to get the plugin from.</param>
    /// <returns>The associated plugin instance or null if none exists.</returns>
    internal static TPlugin? GetPlugin<T, TPlugin>(object pipeline) where TPlugin : class, IPipelinePlugin<T>
    {
        var pluginType = typeof(TPlugin);

        if (_pluginMap.TryGetValue(pipeline, out var pipelinePlugins) && 
            pipelinePlugins.TryGetValue(pluginType, out var pluginObj) && 
            pluginObj is TPlugin typedPlugin)
        {
            return typedPlugin;
        }

        return null;
    }

    /// <summary>
    /// Removes all plugins associated with a pipeline and notifies them of disposal.
    /// </summary>
    /// <param name="pipeline">The pipeline to clean up plugins for.</param>
    internal static void CleanupPipeline<T>(ChannelPipeline<T> pipeline)
    {
        if (_pluginMap.TryRemove(pipeline, out var plugins))
        {
            foreach (var pluginObj in plugins.Values)
            {
                // Try to cast to the correct generic type
                if (pluginObj is IPipelinePlugin<T> typedPlugin)
                {
                    typedPlugin.OnDispose(pipeline);
                }
            }
        }
    }
}
