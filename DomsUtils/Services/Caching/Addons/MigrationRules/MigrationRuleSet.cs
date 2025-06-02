using DomsUtils.Services.Caching.Interfaces.Bases;

namespace DomsUtils.Services.Caching.Addons.MigrationRules;

/// <summary>
/// Represents a collection of rules governing data migration between cache tiers.
/// </summary>
/// <typeparam name="TKey">The type of the key used for cache entries.</typeparam>
/// <typeparam name="TValue">The type of the value used for cache entries.</typeparam>
public class MigrationRuleSet<TKey, TValue> : IMigrationRule<TKey, TValue>
{
    /// <summary>
    /// A private collection that stores migration rules defining conditions for moving
    /// data between tiers in a tiered caching system.
    /// </summary>
    private readonly List<MigrationRule<TKey, TValue>> _rules = new List<MigrationRule<TKey, TValue>>();

    /// <summary>
    /// Specifies the optional interval for periodically triggering the migration check.
    /// If set, the migration rules will be periodically evaluated and data will
    /// migrate between tiers if conditions are met.
    /// </summary>
    /// <remarks>
    /// This property defines the time duration between periodic invocations of
    /// the `CheckAndMigrateAll` method on the `TieredCache`.
    /// If set to null, periodic migration checks are disabled, and migrations will
    /// only occur when explicitly triggered.
    /// </remarks>
    public TimeSpan? PeriodicCheckInterval { get; private set; }

    /// <summary>
    /// Adds a migration rule for transferring entries between two tiers when a specified condition is satisfied.
    /// </summary>
    /// <param name="fromTier">The tier from which the entries will be moved.</param>
    /// <param name="toTier">The tier to which the entries will be moved.</param>
    /// <param name="condition">The condition that determines whether the migration should occur for each entry.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fromTier"/> and <paramref name="toTier"/> are the same.</exception>
    public void AddRule(int fromTier, int toTier, Func<TKey, TValue, ICache<TKey, TValue>, ICache<TKey, TValue>, bool> condition)
    {
        if (fromTier == toTier)
            throw new ArgumentException("fromTier and toTier must be different.");

        _rules.Add(new MigrationRule<TKey, TValue>(fromTier, toTier, condition));
    }

    /// <summary>
    /// Sets the periodic interval for triggering automatic migration checks between tiers in the cache.
    /// </summary>
    /// <param name="interval">The time span interval at which to periodically trigger migration checks.</param>
    public void SetPeriodicInterval(TimeSpan interval)
    {
        PeriodicCheckInterval = interval;
    }

    /// <summary>
    /// Determines whether a key/value pair should migrate between two tiers based on the defined migration rules.
    /// If no specific rule matches, the method returns true by default.
    /// </summary>
    /// <param name="key">The key of the data entry being evaluated for migration.</param>
    /// <param name="value">The value of the data entry being evaluated for migration.</param>
    /// <param name="fromTier">The index of the tier from which the data entry might migrate.</param>
    /// <param name="toTier">The index of the tier to which the data entry might migrate.</param>
    /// <param name="fromCache">The source cache instance.</param>
    /// <param name="toCache">The target cache instance.</param>
    /// <returns>
    /// A boolean indicating if the key/value pair should migrate from the specified source tier to the target tier.
    /// </returns>
    public bool ShouldMigrate(TKey key, TValue value, int fromTier, int toTier, ICache<TKey, TValue> fromCache, ICache<TKey, TValue> toCache)
    {
        foreach (MigrationRule<TKey, TValue> rule in _rules)
        {
            if (rule.ShouldMigrate(key, value, fromTier, toTier, fromCache, toCache))
                return true;
        }

        return true;
    }

    /// <summary>
    /// Retrieves the migration rules that include the specified tier, either as the source or destination tier for data movement.
    /// </summary>
    /// <param name="tierIndex">The index of the tier for which to retrieve the associated migration rules.</param>
    /// <returns>A collection of migration rules that involve the specified tier.</returns>
    public IEnumerable<MigrationRule<TKey, TValue>> GetRulesForTier(int tierIndex)
    {
        return _rules.Where(r => r.FromTier == tierIndex || r.ToTier == tierIndex);
    }
}