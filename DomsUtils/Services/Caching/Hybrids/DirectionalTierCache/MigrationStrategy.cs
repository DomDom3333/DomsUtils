namespace DomsUtils.Services.Caching.Hybrids.DirectionalTierCache;

/// <summary>
/// Defines the strategy for migrating cache entries between tiers in a multi-tier caching system.
/// • PromoteTowardPrimary: Moves items step-by-step upward, in the direction of the primary/first tier.
/// • DemoteTowardSecondary: Moves items step-by-step downward, toward the secondary/last tier.
/// </summary>
public enum MigrationStrategy
{
    /// <summary>
    /// Specifies that, when migrating cached items between tiers, entries should be promoted
    /// step-by-step toward the primary tier (the tier checked first during cache lookup).
    /// </summary>
    PromoteTowardPrimary,

    /// <summary>
    /// Specifies the migration strategy where entries are moved step-by-step
    /// toward the "secondary" tier, relative to the tier check order.
    /// This strategy is typically used to demote entries from higher-priority
    /// tiers to lower-priority tiers when conditions demand a reverse migration.
    /// </summary>
    DemoteTowardSecondary
}