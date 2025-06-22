namespace DomsUtils.Services.Pipeline.Plugins;

/// <summary>
/// Defines a plugin that can be attached to a ChannelPipeline to extend its functionality.
/// </summary>
/// <typeparam name="T">The type of data processed by the pipeline.</typeparam>
public interface IPipelinePlugin<T>
{
    /// <summary>
    /// Gets the name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called when the plugin is attached to a pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline to which the plugin is being attached.</param>
    void OnAttach(ChannelPipeline<T> pipeline);

    /// <summary>
    /// Called when the pipeline is being disposed.
    /// </summary>
    /// <param name="pipeline">The pipeline that is being disposed.</param>
    void OnDispose(ChannelPipeline<T> pipeline);
}
