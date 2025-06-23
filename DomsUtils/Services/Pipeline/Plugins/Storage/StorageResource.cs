namespace DomsUtils.Services.Pipeline.Plugins.Storage;

/// <summary>
/// Adapts storage to the IPipelineResource interface to handle pipeline lifecycle events.
/// </summary>
/// <remarks>
/// This class allows storage implementations to be registered with the PipelineResourceRegistry
/// and be notified when pipelines are disposed.
/// </remarks>
internal class StorageResource : IPipelineResource
{
    /// <summary>
    /// Called when the associated pipeline is being disposed.
    /// </summary>
    /// <param name="pipeline">The pipeline that is being disposed.</param>
    public void OnPipelineDisposing(object pipeline)
    {
        // Clean up storage associated with the pipeline
        PipelineStorageRegistry.CleanupPipeline(pipeline);
    }
}
