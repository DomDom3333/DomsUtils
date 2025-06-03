namespace DomsUtils.Services.Caching.Interfaces.Addons;

/// <summary>
/// Represents a logging interface for managing records of cache deletions,
/// allowing for deletion tracking, querying, and cleanup operations.
/// </summary>
/// <typeparam name="TKey">The type of the keys used in the cache.</typeparam>
public interface ICacheDeletionLog<TKey>
{
    /// <summary>
    /// Logs the deletion of a cache entry identified by the given key at the specified deletion time.
    /// </summary>
    /// <param name="key">The key of the cache entry that was deleted.</param>
    /// <param name="deletionTime">The timestamp indicating when the deletion occurred.</param>
    void LogDeletion(TKey key, DateTimeOffset deletionTime);

    /// <summary>
    /// Determines whether the specified key has been marked as deleted after a given timestamp.
    /// </summary>
    /// <param name="key">The key to check for deletion.</param>
    /// <param name="afterTime">The timestamp after which the key should be considered deleted.</param>
    /// <returns>
    /// true if the key has been marked as deleted after the specified timestamp; otherwise, false.
    /// </returns>
    bool IsDeleted(TKey key, DateTimeOffset afterTime);

    /// <summary>
    /// Cleans up entries from the deletion log that are older than the specified timestamp.
    /// </summary>
    /// <param name="olderThan">The timestamp indicating the cutoff. Entries with deletion times older than this are removed.</param>
    void CleanupDeletionLog(DateTimeOffset olderThan);
}