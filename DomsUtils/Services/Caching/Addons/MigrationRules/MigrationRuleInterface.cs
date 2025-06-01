using DomsUtils.Services.Caching.Interfaces.Bases;

namespace DomsUtils.Services.Caching.Addons.MigrationRules;

/// <summary>
/// Interface defining a rule for determining whether a data entry should be migrated between cache tiers in a tiered caching system.
/// </summary>
/// <typeparam name="TKey">The type of the key for the cached data.</typeparam>
/// <typeparam name="TValue">The type of the value for the cached data.</typeparam>
public interface IMigrationRule<TKey, TValue>
{
    /// <summary>
    /// Determines whether a key/value pair should migrate between two tiers based on specific conditions or rules.
    /// </summary>
    /// <param name="key">The key of the entry being evaluated for migration.</param>
    /// <param name="value">The value associated with the key being evaluated for migration.</param>
    /// <param name="fromTier">The source tier from which the entry might be migrated.</param>
    /// <param name="toTier">The destination tier to which the entry might be migrated.</param>
    /// <param name="fromCache">The cache instance representing the source tier.</param>
    /// <param name="toCache">The cache instance representing the destination tier.</param>
    /// <returns>
    /// A boolean value indicating whether the key/value pair should migrate from the source tier to the destination tier.
    /// </returns>
    bool ShouldMigrate(
        TKey key,
        TValue value,
        int fromTier,
        int toTier,
        ICache<TKey, TValue> fromCache,
        ICache<TKey, TValue> toCache);
}