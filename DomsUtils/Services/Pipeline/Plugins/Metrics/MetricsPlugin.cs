using System.Collections.Concurrent;
using System.Diagnostics;
using DomsUtils.Services.Pipeline.Plugins.Core;

namespace DomsUtils.Services.Pipeline.Plugins;

/// <summary>
/// Plugin that collects simple metrics about pipeline execution.
/// Provides a block modifier to track success, errors and execution time.
/// </summary>
/// <typeparam name="T">The type processed by the pipeline.</typeparam>
public class MetricsPlugin<T> : IPipelinePlugin<T>
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, Stopwatch> _timers = new();

    /// <inheritdoc />
    public string Name => "MetricsPlugin";

    /// <summary>
    /// Create a modifier that records metrics for the specified operation name.
    /// </summary>
    public BlockModifier<T> TrackMetrics(string operationName)
    {
        return next => async (env, ct) =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await next(env, ct);
                IncrementCounter($"{operationName}.success");
                return result;
            }
            catch (Exception)
            {
                IncrementCounter($"{operationName}.error");
                throw;
            }
            finally
            {
                sw.Stop();
                RecordExecutionTime(operationName, sw.ElapsedMilliseconds);
            }
        };
    }

    /// <summary>
    /// Increment a named counter.
    /// </summary>
    public void IncrementCounter(string name, long amount = 1)
    {
        _counters.AddOrUpdate(name, amount, (_, cur) => cur + amount);
    }

    /// <summary>
    /// Record execution time for an operation.
    /// </summary>
    public void RecordExecutionTime(string operation, long ms)
    {
        IncrementCounter($"{operation}.time.total", ms);
        IncrementCounter($"{operation}.time.count");
    }

    /// <summary>
    /// Retrieve collected metrics.
    /// </summary>
    public IReadOnlyDictionary<string, long> GetMetrics() => _counters;

    /// <summary>
    /// Get average execution time for an operation.
    /// </summary>
    public double GetAverageExecutionTime(string operation)
    {
        return _counters.TryGetValue($"{operation}.time.total", out var total) &&
               _counters.TryGetValue($"{operation}.time.count", out var count) &&
               count > 0
            ? (double)total / count
            : 0;
    }

    public void OnAttach(ChannelPipeline<T> pipeline)
    {
        _timers["pipeline.lifetime"] = Stopwatch.StartNew();
    }

    public void OnDispose(ChannelPipeline<T> pipeline)
    {
        if (_timers.TryRemove("pipeline.lifetime", out var sw))
        {
            sw.Stop();
            _counters["pipeline.lifetime.ms"] = sw.ElapsedMilliseconds;
        }
    }
}
