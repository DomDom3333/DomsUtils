using DomsUtils.Services.Caching.Bases;
using DomsUtils.Services.Caching.Interfaces.Addons;
using DomsUtils.Services.Caching.Interfaces.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DomsUtils.Services.Caching.Hybrids.DirectionalTierCache;

/// <summary>
/// Represents a tiered caching mechanism that organizes caches in directional tiers, allowing migration
/// of data between tiers based on a specified migration strategy and direction.
/// </summary>
/// <typeparam name="TKey">The type of the keys used to identify cache entries.</typeparam>
/// <typeparam name="TValue">The type of the values stored in the cache.</typeparam>
/// <remarks>
/// This class enables the use of multiple cache tiers for organizing and managing cached data. Each tier
/// can represent a different kind of cache (e.g., in-memory, distributed) and the data can be migrated
/// between tiers based on the provided migration direction and strategy.
/// </remarks>
public sealed class DirectionalTierCache<TKey, TValue> : ICache<TKey, TValue>, ICacheAvailability, ICacheMigratable, IDisposable, IAsyncDisposable
{
    private readonly ICache<TKey, TValue>[] _tiers;
    private readonly CacheDirection _cacheDirection;
    private readonly MigrationStrategy _migrationStrategy;
    private readonly Timer? _migrationTimer;
    private readonly ILogger _logger;

    /// <summary>
    /// Represents a caching mechanism that uses multiple cache tiers with directional priority.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of keys used to identify values in the cache.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values stored in the cache.
    /// </typeparam>
    /// <remarks>
    /// This class supports configurable cache directional priorities, migration strategies,
    /// and optional automatic migration between tiers based on a specified interval.
    /// </remarks>
    public DirectionalTierCache(
        CacheDirection cacheDirection,
        MigrationStrategy migrationStrategy,
        TimeSpan? migrationInterval,
        ILogger logger = default,
        params ICache<TKey, TValue>[] tiers)
    {
        if (tiers == null || tiers.Length == 0)
            throw new ArgumentException("At least one tier must be provided.", nameof(tiers));

        _tiers = new List<ICache<TKey, TValue>>(tiers).ToArray();
        _cacheDirection = cacheDirection;
        _migrationStrategy = migrationStrategy;
        _logger = logger ?? NullLogger.Instance;

        _logger.LogInformation("DirectionalTierCache initialized with {TierCount} tiers.", _tiers.Length);

        if (migrationInterval.HasValue && migrationInterval.Value > TimeSpan.Zero)
        {
            // Delay first tick by _migrationInterval, then repeat at that interval
            _migrationTimer = new Timer(_ => CheckAndMigrateAll(), null, migrationInterval.Value, migrationInterval.Value);
            _logger.LogInformation("Migration timer started with interval {Interval}.", migrationInterval.Value);
        }
    }

    /// <summary>
    /// True if any underlying tier is available (or doesn’t implement ICacheAvailability).
    /// </summary>
    private bool IsAvailable
    {
        get
        {
            foreach (ICache<TKey, TValue> tier in _tiers)
            {
                if (tier is ICacheAvailability a)
                {
                    if (a.IsAvailable()) return true;
                }
                else
                {
                    // Assume available if no availability interface
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Attempts to retrieve a value associated with the specified key from the cache tiers based on the configured cache direction.
    /// </summary>
    /// <param name="key">
    /// The key associated with the value to retrieve.
    /// </param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value associated with the specified key is found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGet(TKey key, out TValue value)
    {
        _logger.LogDebug("Attempting to retrieve key '{Key}' from DirectionalTierCache.", key);
        TValue foundValue = default!;
        bool found = ExecuteOnTiers(tier =>
        {
            if (tier.TryGet(key, out TValue tempValue))
            {
                foundValue = tempValue;
                return true;
            }
            return false;
        });

        value = foundValue;
        if (found)
            _logger.LogDebug("Key '{Key}' found in DirectionalTierCache.", key);
        else
            _logger.LogDebug("Key '{Key}' not found in DirectionalTierCache.", key);

        return found;
    }

    /// <summary>
    /// Writes to the highest‐priority tier that is currently available.
    /// That "highest‐priority" is determined by _cacheDirection_:
    /// • LowToHigh:  attempt index 0, then 1, …, up to N−1.  
    /// • HighToLow:  attempt index N−1, then N−2, …, down to 0.  
    /// Stop after the first successful write.
    /// </summary>
    public void Set(TKey key, TValue value)
    {
        _logger.LogDebug("Setting key '{Key}' in DirectionalTierCache.", key);
        ExecuteOnTiers(tier =>
        {
            try
            {
                tier.Set(key, value);
                _logger.LogDebug("Key '{Key}' set in a tier.", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting key '{Key}' in a tier.", key);
                return false;
            }
        });
    }

    /// <summary>
    /// Attempts to remove from all tiers that are available (or do not implement availability).
    /// Returns true if any tier's Remove(key) returned true.
    /// </summary>
    public bool Remove(TKey key)
    {
        _logger.LogDebug("Removing key '{Key}' from DirectionalTierCache.", key);
        bool removedAny = false;
        foreach (ICache<TKey, TValue> tier in _tiers)
        {
            if (tier is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Tier is unavailable for removing key '{Key}'.", key);
                continue;
            }

            try
            {
                removedAny |= tier.Remove(key);
                _logger.LogDebug("Key '{Key}' removed from a tier.", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing key '{Key}' from a tier.", key);
            }
        }

        if (removedAny)
            _logger.LogInformation("Key '{Key}' successfully removed from one or more tiers.", key);
        else
            _logger.LogWarning("Key '{Key}' not found in any tier for removal.", key);

        return removedAny;
    }

    /// <summary>
    /// Clears all tiers that are currently available.
    /// </summary>
    public void Clear()
    {
        _logger.LogWarning("Clearing all tiers in DirectionalTierCache.");
        foreach (ICache<TKey, TValue> tier in _tiers)
        {
            if (tier is ICacheAvailability avail && !avail.IsAvailable())
            {
                _logger.LogWarning("Tier is unavailable for clearing.");
                continue;
            }

            try
            {
                tier.Clear();
                _logger.LogInformation("Tier cleared successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing a tier.");
            }
        }
    }

    /// <summary>
    /// Evaluates and migrates entries across all tiers of the cache based on the defined migration strategy
    /// and specified cache direction.
    /// </summary>
    /// <remarks>
    /// The method iterates through each cache tier, identifying valid neighboring tiers for migration
    /// and moving entries accordingly. It respects the configured migration strategy and cache direction,
    /// enabling dynamic promotion or demotion of entries between tiers. This process ensures that cache entries
    /// are optimally allocated across different tiers to maintain performance and accessibility.
    /// </remarks>
    private void CheckAndMigrateAll()
    {
        _logger.LogDebug("Starting migration across all tiers in DirectionalTierCache.");
        int count = _tiers.Length;
        if (count < 2) return;

        // Early optimization: Check if there are any keys to migrate before processing
        if (!HasKeysToMigrate())
        {
            _logger.LogDebug("No keys found in source tiers, skipping migration.");
            return;
        }

        MigrationParameters migrationParams = CalculateMigrationParameters(count);

        for (int i = migrationParams.Start; ShouldContinueIteration(i, migrationParams); i += migrationParams.Step)
        {
            int targetIndex = i + migrationParams.NeighborOffset;
            MigrateBetweenTiers(i, targetIndex);
        }
    }

    /// <summary>
    /// Checks if there are any keys available for migration in the source tiers.
    /// </summary>
    /// <returns>
    /// True if any source tier contains keys that could potentially be migrated; otherwise, false.
    /// </returns>
    private bool HasKeysToMigrate()
    {
        int count = _tiers.Length;
        MigrationParameters migrationParams = CalculateMigrationParameters(count);

        for (int i = migrationParams.Start; ShouldContinueIteration(i, migrationParams); i += migrationParams.Step)
        {
            int targetIndex = i + migrationParams.NeighborOffset;
            
            // Check if target index is valid
            if (targetIndex < 0 || targetIndex >= count) continue;
            
            ICache<TKey, TValue> sourceTier = _tiers[i];
            ICache<TKey, TValue> targetTier = _tiers[targetIndex];

            if (!CanMigrateBetweenTiers(sourceTier, targetTier))
                continue;

            // Quick check if source tier has any keys
            if (HasKeysInTier(sourceTier))
            {
                _logger.LogDebug("Found keys in source tier {SourceIndex}, migration will proceed.", i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Performs a lightweight check to determine if a cache tier contains any keys.
    /// </summary>
    /// <param name="tier">The cache tier to check for keys.</param>
    /// <returns>True if the tier contains keys; otherwise, false.</returns>
    private bool HasKeysInTier(ICache<TKey, TValue> tier)
    {
        try
        {
            IEnumerable<TKey>? keys = GetKeysFromSource(tier);
            if (keys == null) return false;

            // Use Any() for efficient existence check without enumerating all keys
            return keys.Any();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking if tier has keys, assuming no keys available.");
            return false;
        }
    }

    /// <summary>
    /// Executes an operation on cache tiers in the configured direction until the operation succeeds or all tiers are exhausted.
    /// </summary>
    /// <param name="operation">
    /// A function that takes a cache tier and returns true if the operation succeeded and iteration should stop, false to continue to the next tier.
    /// </param>
    /// <returns>
    /// True if the operation succeeded on any tier, false if all tiers were exhausted without success.
    /// </returns>
    private bool ExecuteOnTiers(Func<ICache<TKey, TValue>, bool> operation)
    {
        int count = _tiers.Length;
        IEnumerable<int> indices = GetTierIndices(count);

        foreach (int i in indices)
        {
            ICache<TKey, TValue> tier = _tiers[i];
            if (tier is ICacheAvailability a && !a.IsAvailable())
            {
                _logger.LogWarning("Tier {TierIndex} is unavailable.", i);
                continue;
            }

            try
            {
                if (operation(tier))
                {
                    _logger.LogDebug("Operation succeeded on tier {TierIndex}.", i);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing operation on tier {TierIndex}.", i);
            }
        }

        _logger.LogDebug("Operation failed on all tiers.");
        return false;
    }

    /// <summary>
    /// Gets the sequence of tier indices based on the configured cache direction.
    /// </summary>
    /// <param name="count">The total number of tiers.</param>
    /// <returns>An enumerable of indices in the correct order for the cache direction.</returns>
    private IEnumerable<int> GetTierIndices(int count)
    {
        if (_cacheDirection == CacheDirection.LowToHigh)
        {
            for (int i = 0; i < count; i++)
                yield return i;
        }
        else // HighToLow
        {
            for (int i = count - 1; i >= 0; i--)
                yield return i;
        }
    }

    /// <summary>
    /// Calculates migration parameters for traversing cache tiers during the migration process.
    /// </summary>
    /// <param name="tierCount">
    /// The total number of tiers in the cache.
    /// </param>
    /// <returns>
    /// A <see cref="MigrationParameters"/> structure containing start, end, step values, and neighbor offset
    /// for navigating tiers during migration.
    /// </returns>
    private MigrationParameters CalculateMigrationParameters(int tierCount)
    {
        bool promote = (_migrationStrategy == MigrationStrategy.PromoteTowardPrimary);
        bool lowToHigh = (_cacheDirection == CacheDirection.LowToHigh);

        int neighborOffset = CalculateNeighborOffset(promote, lowToHigh);
        (int start, int end, int step) = CalculateIterationBounds(neighborOffset, tierCount);

        return new MigrationParameters(start, end, step, neighborOffset);
    }

    /// <summary>
    /// Determines the neighbor offset for migration operations between cache tiers based on
    /// the specified promotion or demotion strategy and cache directional flow.
    /// </summary>
    /// <param name="promote">
    /// A boolean value indicating whether the migration strategy is to promote towards the primary cache.
    /// If false, the strategy is to demote towards the secondary cache.
    /// </param>
    /// <param name="lowToHigh">
    /// A boolean value indicating the direction of the cache tiers. If true, the tiers are ordered
    /// from a lower to higher priority (LowToHigh); otherwise, from higher to lower priority (HighToLow).
    /// </param>
    /// <returns>
    /// An integer representing the computed neighbor offset. A positive offset indicates forwarding to
    /// the next tier, while a negative offset indicates reversing to
    /// the previous tier.
    /// </returns>
    private static int CalculateNeighborOffset(bool promote, bool lowToHigh)
    {
        // PromoteTowardPrimary & LowToHigh  => neighborOffset = +1
        // PromoteTowardPrimary & HighToLow  => neighborOffset = -1
        // DemoteTowardSecondary & LowToHigh => neighborOffset = -1
        // DemoteTowardSecondary & HighToLow => neighborOffset = +1
        if (promote)
            return lowToHigh ? +1 : -1;
        else
            return lowToHigh ? -1 : +1;
    }

    /// <summary>
    /// Calculates the iteration bounds for traversing tiers in the cache based on the neighbor offset and tier count.
    /// </summary>
    /// <param name="neighborOffset">
    /// The offset indicating the direction of traversal between neighboring tiers. A positive value denotes
    /// traversal from a lower index to a higher index, while a negative value denotes the opposite.
    /// </param>
    /// <param name="tierCount">
    /// The total number of tiers in the cache.
    /// </param>
    /// <returns>
    /// A tuple containing the start index, end index, and step value for iterating through the tiers.
    /// </returns>
    private static (int Start, int End, int Step) CalculateIterationBounds(int neighborOffset, int tierCount)
    {
        if (neighborOffset > 0)
        {
            return (Start: 0, End: tierCount - 1, Step: +1);
        }
        else
        {
            return (Start: tierCount - 1, End: 0, Step: -1);
        }
    }

    /// <summary>
    /// Determines whether the iteration should continue based on the current index
    /// and the specified migration parameters.
    /// </summary>
    /// <param name="currentIndex">
    /// The current index of iteration.
    /// </param>
    /// <param name="parameters">
    /// The parameters defining the start, end, step, and neighbor offset for the migration operation.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the iteration should proceed.
    /// </returns>
    private static bool ShouldContinueIteration(int currentIndex, MigrationParameters parameters)
    {
        return (parameters.Step > 0 && currentIndex < parameters.End) ||
               (parameters.Step < 0 && currentIndex > parameters.End);
    }

    /// <summary>
    /// Handles the migration of cache items between two specified cache tiers.
    /// </summary>
    /// <param name="sourceIndex">
    /// The zero-based index of the source tier from which items will be migrated.
    /// </param>
    /// <param name="targetIndex">
    /// The zero-based index of the target tier to which items will be migrated.
    /// </param>
    /// <remarks>
    /// This method ensures that items meeting specific migration criteria are moved from
    /// the source cache tier to the target cache tier. The process considers eligibility
    /// rules, migration success, and gracefully aborts if failures occur.
    /// </remarks>
    private void MigrateBetweenTiers(int sourceIndex, int targetIndex)
    {
        ICache<TKey, TValue> sourceTier = _tiers[sourceIndex];
        ICache<TKey, TValue> targetTier = _tiers[targetIndex];

        if (!CanMigrateBetweenTiers(sourceTier, targetTier))
            return;

        IEnumerable<TKey>? keys = GetKeysFromSource(sourceTier);
        if (keys == null) return;

        foreach (TKey key in keys)
        {
            if (!ShouldMigrateKey(key, targetTier))
                continue;

            if (!TryMigrateKey(key, sourceTier, targetTier))
                return; // Abort migration on failure
        }
    }

    /// <summary>
    /// Determines whether migration is allowed between two cache tiers based on their availability and functionality.
    /// </summary>
    /// <param name="sourceTier">
    /// The source tier of the cache from which data will be migrated.
    /// </param>
    /// <param name="targetTier">
    /// The target tier of the cache to which data will be migrated.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether data migration can occur between the specified source and target tiers.
    /// </returns>
    private static bool CanMigrateBetweenTiers(ICache<TKey, TValue> sourceTier, ICache<TKey, TValue> targetTier)
    {
        // Must have an enumerable source
        if (sourceTier is not ICacheEnumerable<TKey>)
            return false;

        // Target must be available (if it declares availability)
        if (targetTier is ICacheAvailability targetAvail && !targetAvail.IsAvailable())
            return false;

        // Source tier itself must be available
        if (sourceTier is ICacheAvailability sourceAvail && !sourceAvail.IsAvailable())
            return false;

        return true;
    }

    /// <summary>
    /// Retrieves keys from a specified cache tier if it supports enumeration of keys.
    /// </summary>
    /// <param name="sourceTier">
    /// The cache tier from which to retrieve the keys. Expected to implement <see cref="ICacheEnumerable{TKey}"/>.
    /// </param>
    /// <returns>
    /// A collection of keys if the tier supports key enumeration; otherwise, null.
    /// </returns>
    private IEnumerable<TKey>? GetKeysFromSource(ICache<TKey, TValue> sourceTier)
    {
        try
        {
            // Check if this is the specialized S3Cache<TValue> (where TKey is string)
            if (sourceTier is S3Cache<TValue> s3StringCache && typeof(TKey) == typeof(string))
            {
                // Use dynamic to call the overridden Keys() method on S3Cache<TValue>
                dynamic dynamicCache = s3StringCache;
                dynamic? keys = dynamicCache.Keys();
                return keys as IEnumerable<TKey>;
            }
            // Check if source implements ICacheEnumerable<TKey>
            else if (sourceTier is ICacheEnumerable<TKey> sourceEnum)
            {
                return sourceEnum.Keys();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving keys from cache tier");
            return null; // If enumeration fails, skip this source tier
        }
    }

    /// <summary>
    /// Determines whether a specific key should be migrated to a target cache tier.
    /// </summary>
    /// <param name="key">
    /// The key to be evaluated for migration.
    /// </param>
    /// <param name="targetTier">
    /// The target cache tier where the key would be migrated.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the key should be migrated to the target tier.
    /// Returns true if the key does not already exist in the target tier; otherwise, false.
    /// </returns>
    private bool ShouldMigrateKey(TKey key, ICache<TKey, TValue> targetTier)
    {
        try
        {
            return !targetTier.TryGet(key, out _);
        }
        catch (Exception ex)
        {
            // Expected behavior for missing keys in some cache implementations (e.g., S3)
            // Log as debug instead of error since this is normal during migration
            _logger.LogDebug(ex, "Expected exception while checking if key exists in target tier for migration");
            return false; // If probing target fails, don't migrate this key
        }
    }

    /// <summary>
    /// Attempts to migrate a specified key and its associated value from a source cache tier to a target cache tier.
    /// </summary>
    /// <param name="key">
    /// The key to be migrated.
    /// </param>
    /// <param name="sourceTier">
    /// The cache tier from which the key and its associated value are to be migrated.
    /// </param>
    /// <param name="targetTier">
    /// The cache tier to which the key and its associated value are to be migrated.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the migration is successful; otherwise, returns <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method ensures that the value associated with the specified key is successfully moved from the source tier
    /// to the target tier. If the migration fails at any point, the method will return <c>false</c>.
    /// </remarks>
    private bool TryMigrateKey(TKey key, ICache<TKey, TValue> sourceTier, ICache<TKey, TValue> targetTier)
    {
        try
        {
            // First, try to get the value from the source tier
            if (!sourceTier.TryGet(key, out TValue? value))
            {
                // Key doesn't exist in source, nothing to migrate
                return false;
            }

            // Attempt to set the value in the target tier
            targetTier.Set(key, value);

            // Verify the migration was successful by checking if the key exists in target
            if (targetTier.TryGet(key, out _))
            {
                // Migration successful - now remove from source
                bool removedFromSource = sourceTier.Remove(key);
            
                if (!removedFromSource)
                {
                    // Log warning but don't fail the migration since data is in target
                    _logger?.LogWarning("Failed to remove key {Key} from source tier after successful migration", key);
                }
            
                return true;
            }
            else
            {
                // Migration failed - value not found in target tier
                _logger?.LogError("Migration verification failed for key {Key} - value not found in target tier", key);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to migrate key {Key} between tiers", key);
            return false;
        }
    }

    private readonly record struct MigrationParameters(int Start, int End, int Step, int NeighborOffset);

    /// <summary>
    /// Releases unmanaged resources associated with the current instance of the cache.
    /// </summary>
    /// <remarks>
    /// This method is invoked internally during the disposal process to ensure proper cleanup
    /// of unmanaged resources, such as handles or connections, preventing potential resource leaks.
    /// </remarks>
    private void ReleaseUnmanagedResources()
    {
        try
        {
            // Add null check for _tiers array to prevent finalizer crashes
            if (_tiers != null)
            {
                // If any of the underlying cache tiers hold unmanaged resources
                // and implement IDisposable, release them here.
                foreach (ICache<TKey, TValue> tier in _tiers)
                {
                    // Add null check for individual tier
                    if (tier is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch
                        {
                            // Optionally log the error; swallow to avoid throwing during cleanup.
                            // Note: Don't use _logger here as it might be null during finalization
                        }
                    }
                }
            }
        }
        catch
        {
            // Suppress all exceptions in finalizer to prevent process crashes
            // This is critical when called from the finalizer (~DirectionalTierCache)
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="DirectionalTierCache{TKey, TValue}"/> instance.
    /// </summary>
    /// <param name="disposing">
    /// A boolean value indicating whether the method is called during explicit disposal (<c>true</c>)
    /// or during finalization (<c>false</c>).
    /// </param>
    /// <remarks>
    /// This method ensures the proper cleanup of managed and, if necessary, unmanaged resources
    /// associated with the cache tiers. It is called by the public <see cref="Dispose()"/> method
    /// for explicit disposal and during the finalizer process for garbage collection.
    /// </remarks>
    public void Dispose(bool disposing)
    {
        try
        {
            ReleaseUnmanagedResources();
            
            if (disposing)
            {
                // Only dispose managed resources when called from Dispose(), not from finalizer
                _migrationTimer?.Dispose();
            }
        }
        catch
        {
            // Suppress exceptions during disposal, especially in finalizer
            if (!disposing)
            {
                // We're in the finalizer, absolutely no exceptions allowed
            }
            else
            {
                // Re-throw if called from explicit Dispose() so caller knows there was an issue
                throw;
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="DirectionalTierCache{TKey, TValue}"/> instance.
    /// </summary>
    /// <remarks>
    /// This method is called to explicitly release both managed and unmanaged resources used by the cache.
    /// It ensures proper resource cleanup and suppresses finalization to optimize garbage collection.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Represents a multi-tier caching system with directional priority and migration capabilities.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key used to identify cached items.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the value stored in the cache.
    /// </typeparam>
    /// <remarks>
    /// This class provides functionality for managing cache items across multiple tiers with a specified direction
    /// (e.g., lower-tier-to-higher-tier or higher-tier-to-lower-tier) and supports automatic or manual migration of items based on a defined strategy and interval.
    /// It implements interfaces to check availability, manage the lifecycle of resources, and allow asynchronous disposal.
    /// </remarks>
    ~DirectionalTierCache()
    {
        Dispose(false);
    }


    /// <summary>
    /// Indicates whether the cache is currently available for operations.
    /// </summary>
    /// <returns>
    /// A boolean value where true indicates that the cache is available, and false indicates it is not.
    /// </returns>
    bool ICacheAvailability.IsAvailable()
    {
        return IsAvailable;
    }

    /// <summary>
    /// Asynchronously releases the unmanaged resources used by the cache and performs cleanup operations.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous dispose operation.
    /// </returns>
    private async ValueTask DisposeAsyncCore()
    {
        try
        {
            ReleaseUnmanagedResources();

            if (_migrationTimer != null) 
                await _migrationTimer.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            // Suppress exceptions during async disposal
            // Async disposal should not throw
        }
    }

    /// <summary>
    /// Asynchronously releases all resources used by the <see cref="DirectionalTierCache{TKey, TValue}"/> and its underlying resources.
    /// </summary>
    /// <remarks>
    /// This method ensures proper disposal of asynchronous resources and suppresses finalization
    /// to optimize garbage collection. It should be used when asynchronous cleanup is necessary.
    /// </remarks>
    /// <returns>
    /// A task that represents the asynchronous dispose operation.
    /// </returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Triggers an immediate reevaluation and migration (promotion/demotion) of cache entries across all tiers.
    /// </summary>
    public void TriggerMigrationNow()
    {
        _logger.LogInformation("Manual migration trigger invoked.");
        CheckAndMigrateAll();
    }
}