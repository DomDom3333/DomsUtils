namespace DomsUtils.Services.Caching.Hybrids.ParallelCache;

/// <summary>
/// Configuration options for cache synchronization behavior.
/// </summary>
public class SyncOptions
{
    /// <summary>
    /// Strategy for resolving conflicts when caches have different data.
    /// </summary>
    public SyncConflictResolution ConflictResolution { get; set; } = SyncConflictResolution.MajorityWins;

    /// <summary>
    /// Threshold for majority consensus (0.5 = 50% + 1).
    /// </summary>
    public double MajorityThreshold { get; set; } = 0.5;
}