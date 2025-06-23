using System.Collections.Concurrent;

namespace DomsUtils.Services.Pipeline;

/// <summary>
/// Registry that manages resources associated with pipelines.
/// </summary>
/// <remarks>
/// This registry centralizes management of resources that need to be notified
/// when pipelines are disposed, regardless of their specific type.
/// </remarks>
internal static class PipelineResourceRegistry
{
    private static readonly ConcurrentDictionary<object, List<IPipelineResource>> _resourceMap = new();

    /// <summary>
    /// Registers a resource with a pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline to associate the resource with.</param>
    /// <param name="resource">The resource to register.</param>
    internal static void RegisterResource(object pipeline, IPipelineResource resource)
    {
        var resources = _resourceMap.GetOrAdd(pipeline, _ => new List<IPipelineResource>());
        lock (resources)
        {
            resources.Add(resource);
        }
    }

    /// <summary>
    /// Notifies all resources associated with a pipeline that it's being disposed.
    /// </summary>
    /// <param name="pipeline">The pipeline being disposed.</param>
    internal static void NotifyPipelineDisposing(object pipeline)
    {
        if (_resourceMap.TryRemove(pipeline, out var resources))
        {
            foreach (var resource in resources)
            {
                resource.OnPipelineDisposing(pipeline);
            }
        }
    }
}
