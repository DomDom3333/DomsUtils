namespace DomsUtils.Services.Caching.Hybrids.ParallelCache;

/// <summary>
/// Strategies for resolving conflicts during cache synchronization.
/// </summary>
public enum SyncConflictResolution
{
    /// <summary>
    /// The majority of caches determine the correct state.
    /// If most caches don't have a key, it's considered deleted.
    /// </summary>
    MajorityWins,

    /// <summary>
    /// The primary cache (first in the list) always wins conflicts.
    /// </summary>
    PrimaryWins
}