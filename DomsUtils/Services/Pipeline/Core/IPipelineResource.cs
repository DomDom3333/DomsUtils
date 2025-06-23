namespace DomsUtils.Services.Pipeline;

/// <summary>
/// Represents a resource attached to a pipeline that needs to be notified when the pipeline is disposed.
/// </summary>
/// <remarks>
/// This interface allows for decoupling of the pipeline from specific resources like plugins or storage,
/// supporting a more extensible architecture.
/// </remarks>
public interface IPipelineResource
{
    /// <summary>
    /// Called when the associated pipeline is being disposed.
    /// </summary>
    /// <param name="pipeline">The pipeline that is being disposed.</param>
    void OnPipelineDisposing(object pipeline);
}
