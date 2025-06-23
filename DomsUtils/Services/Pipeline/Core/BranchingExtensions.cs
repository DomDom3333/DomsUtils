using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DomsUtils.Services.Pipeline;

/// <summary>
/// Extension methods providing conditional branching for <see cref="ChannelPipeline{T}"/>.
/// </summary>
public static class BranchingExtensions
{
    /// <summary>
    /// Executes different blocks based on a boolean condition.
    /// </summary>
    /// <typeparam name="T">Type of data processed by the pipeline.</typeparam>
    /// <param name="pipeline">The pipeline to add the branching block to.</param>
    /// <param name="condition">Predicate used to choose the branch.</param>
    /// <param name="trueBlock">Block executed when the condition is true.</param>
    /// <param name="falseBlock">Optional block executed when the condition is false. When null the item is passed through.</param>
    /// <returns>The pipeline instance for chaining.</returns>
    public static ChannelPipeline<T> BranchIf<T>(
        this ChannelPipeline<T> pipeline,
        Func<T, bool> condition,
        BlockOptions<T> trueBlock,
        BlockOptions<T>? falseBlock = null)
    {
        if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        if (trueBlock == null) throw new ArgumentNullException(nameof(trueBlock));

        BlockModifier<T>[]? CombineModifiers(IReadOnlyList<BlockModifier<T>>? first, IReadOnlyList<BlockModifier<T>>? second)
        {
            if (first == null && second == null) return null;
            var list = new List<BlockModifier<T>>();
            if (first != null) list.AddRange(first);
            if (second != null) list.AddRange(second);
            return list.ToArray();
        }

        var branchFlag = new AsyncLocal<bool>();

        var options = new BlockOptions<T>
        {
            Parallelism = Math.Max(trueBlock.Parallelism, falseBlock?.Parallelism ?? 1),
            ChannelOptions = trueBlock.ChannelOptions ?? falseBlock?.ChannelOptions,
            CancellationToken = trueBlock.CancellationToken != CancellationToken.None
                ? trueBlock.CancellationToken
                : (falseBlock?.CancellationToken ?? CancellationToken.None),
            Modifiers = CombineModifiers(trueBlock.Modifiers, falseBlock?.Modifiers),
            OnError = ex =>
            {
                var handler = branchFlag.Value ? trueBlock.OnError : falseBlock?.OnError;
                handler?.Invoke(ex);
            },
            AsyncTransform = async (item, ct) =>
            {
                branchFlag.Value = condition(item);
                if (branchFlag.Value)
                    return await trueBlock.AsyncTransform(item, ct);
                else if (falseBlock != null)
                    return await falseBlock.AsyncTransform(item, ct);
                return item;
            }
        };

        return pipeline.AddBlock(options);
    }
}
