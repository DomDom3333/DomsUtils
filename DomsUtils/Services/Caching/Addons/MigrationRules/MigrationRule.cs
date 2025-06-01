using DomsUtils.Services.Caching.Interfaces.Bases;

namespace DomsUtils.Services.Caching.Addons.MigrationRules;

/// <summary>
/// Defines the conditions and settings for migrating data between different tiers in a tiered caching system.
/// </summary>
/// <typeparam name="TKey">Specifies the type of the key for the cached data.</typeparam>
/// <typeparam name="TValue">Specifies the type of the value for the cached data.</typeparam>
public class MigrationRule<TKey, TValue> : IMigrationRule<TKey, TValue>
{
    /// <summary>
    /// Gets the source tier index in a tiered caching system.
    /// Defines the tier from which data originates during a migration process.
    /// </summary>
    public int FromTier { get; }

    /// <summary>
    /// Gets the tier number to which a migration is directed.
    /// Indicates the target tier in the caching hierarchy for a migration rule.
    /// </summary>
    public int ToTier { get; }

    /// <summary>
    /// Gets the condition that determines whether a given key and value should trigger migration
    /// between tiers within the tiered caching system.
    /// This condition serves as a predicate to evaluate specific migration rules.
    /// Includes source and target cache instances for custom logic.
    /// </summary>
    public Func<TKey, TValue, ICache<TKey, TValue>, ICache<TKey, TValue>, bool> Condition { get; }

    /// <summary>
    /// Gets a value indicating whether the migration rule represents a demotion.
    /// A migration is considered a demotion if data is moved from a lower-numbered tier
    /// to a higher-numbered tier within the caching hierarchy.
    /// </summary>
    public bool IsDemotion => FromTier < ToTier;

    /// <summary>
    /// Determines whether the migration represents a promotion,
    /// where data moves to a higher priority or higher-performing tier in the caching hierarchy.
    /// </summary>
    public bool IsPromotion => FromTier > ToTier;

    /// <summary>
    /// Represents a rule that dictates how and when data should be migrated between tiers within a tiered caching system.
    /// </summary>
    /// <typeparam name="TKey">The type of the key associated with the data.</typeparam>
    /// <typeparam name="TValue">The type of the value associated with the data.</typeparam>
    public MigrationRule(
        int fromTier,
        int toTier,
        Func<TKey, TValue, ICache<TKey, TValue>, ICache<TKey, TValue>, bool> condition)
    {
        FromTier = fromTier;
        ToTier = toTier;
        Condition = condition;
    }

    /// <summary>
    /// Determines whether a specified data entry should migrate between cache tiers
    /// based on the defined migration rule.
    /// </summary>
    /// <param name="key">The key identifying the data entry to evaluate for migration.</param>
    /// <param name="value">The value of the data entry to evaluate for migration.</param>
    /// <param name="fromTier">The current cache tier index of the data entry.</param>
    /// <param name="toTier">The target cache tier index for the data entry.</param>
    /// <param name="fromCache">The source cache instance.</param>
    /// <param name="toCache">The target cache instance.</param>
    /// <returns>
    /// A boolean value indicating whether the data entry meets the criteria for migration
    /// as defined by the rule.
    /// </returns>
    public bool ShouldMigrate(TKey key, TValue value, int fromTier, int toTier, ICache<TKey, TValue> fromCache, ICache<TKey, TValue> toCache)
    {
        if (FromTier == fromTier && ToTier == toTier)
            return Condition(key, value, fromCache, toCache);
        return false;
    }
}