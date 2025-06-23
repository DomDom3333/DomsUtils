using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DomsUtils.Services.Pipeline;

/// <summary>
/// Fluent API for building and executing in-memory data-processing pipelines
/// with optional branching and merging.
/// </summary>
public class Pipeline<T>
{
    private readonly List<Step> _preMerge = new();
    private readonly List<Step> _postMerge = new();
    private readonly List<Pipeline<T>> _branches = new();
    private readonly bool _isBranch;

    private bool _splitCalled;
    private bool _mergeCalled;

    private Pipeline(bool isBranch)
    {
        _isBranch = isBranch;
    }

    /// <summary>
    /// Creates a new pipeline builder.
    /// </summary>
    public static Pipeline<T> Create() => new Pipeline<T>(false);

    /// <summary>
    /// Adds a transformation step to the pipeline.
    /// </summary>
    public Pipeline<T> Pipe(Func<T, ValueTask<T>> asyncFn, int parallelism = 1)
    {
        ArgumentNullException.ThrowIfNull(asyncFn);
        if (parallelism <= 0) throw new ArgumentOutOfRangeException(nameof(parallelism));

        if (_splitCalled && !_mergeCalled)
            throw new InvalidOperationException("Cannot add pipe outside of a branch before Merge() is called");

        var step = new Step(asyncFn, parallelism);
        if (!_splitCalled)
            _preMerge.Add(step);
        else
            _postMerge.Add(step);
        return this;
    }

    /// <summary>
    /// Defines branching of the pipeline. Each lambda receives a new pipeline
    /// instance for defining branch steps.
    /// </summary>
    public Pipeline<T> Split(params Func<Pipeline<T>, Pipeline<T>>[] branchDefs)
    {
        if (_isBranch)
            throw new InvalidOperationException("Split cannot be used inside a branch");
        if (branchDefs == null || branchDefs.Length == 0)
            throw new ArgumentException("At least one branch must be specified", nameof(branchDefs));
        if (_splitCalled && !_mergeCalled)
            throw new InvalidOperationException("Merge must be called before another Split");
        if (_mergeCalled)
            throw new InvalidOperationException("Cannot split after merge");

        _splitCalled = true;
        _branches.Clear();
        foreach (var def in branchDefs)
        {
            ArgumentNullException.ThrowIfNull(def);
            var branch = new Pipeline<T>(true);
            _branches.Add(def(branch));
        }
        return this;
    }

    /// <summary>
    /// Merges previously defined branches back into a single stream.
    /// </summary>
    public Pipeline<T> Merge()
    {
        if (_isBranch)
            throw new InvalidOperationException("Merge cannot be used inside a branch");
        if (!_splitCalled)
            throw new InvalidOperationException("No branches to merge");
        if (_mergeCalled)
            throw new InvalidOperationException("Merge already called");

        _mergeCalled = true;
        return this;
    }

    /// <summary>
    /// Executes the pipeline over the given inputs and returns all results.
    /// </summary>
    public async Task<List<T>> RunAsync(IEnumerable<T> inputs, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (_splitCalled && !_mergeCalled)
            throw new InvalidOperationException("Merge() must be called after Split()");

        var preResults = await RunStepsAsync(_preMerge, inputs, ct).ConfigureAwait(false);

        if (!_splitCalled)
        {
            return await RunStepsAsync(_postMerge, preResults, ct).ConfigureAwait(false);
        }

        var branchOutputs = new List<T>();
        foreach (var branch in _branches)
        {
            var res = await RunStepsAsync(branch._preMerge, preResults, ct).ConfigureAwait(false);
            branchOutputs.AddRange(res);
        }

        return await RunStepsAsync(_postMerge, branchOutputs, ct).ConfigureAwait(false);
    }

    private static async Task<List<T>> RunStepsAsync(IEnumerable<Step> steps, IEnumerable<T> inputs, CancellationToken ct)
    {
        if (!steps.Any())
            return inputs.ToList();

        await using var pipeline = new ChannelPipeline<T>();
        foreach (var step in steps)
        {
            pipeline.AddBlock(new BlockOptions<T>
            {
                AsyncTransform = (v, ct2) => step.Transform(v),
                Parallelism = step.Parallelism
            });
        }

        var reader = pipeline.Build();
        foreach (var item in inputs)
            await pipeline.WriteAsync(item, ct);

        await pipeline.CompleteAsync();

        var results = new List<T>();
        await foreach (var item in reader.ReadAllAsync(ct))
            results.Add(item);

        await pipeline.DisposeAsync();
        return results;
    }

    private record Step(Func<T, ValueTask<T>> Transform, int Parallelism);
}

