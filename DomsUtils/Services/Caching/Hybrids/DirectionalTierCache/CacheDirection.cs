namespace DomsUtils.Services.Caching.Hybrids.DirectionalTierCache;

/// <summary>
/// Specifies the direction in which a cache should traverse its tiers during operations such as reading or writing.
/// The two available directions determine the order in which cache tiers are accessed:
/// • LowToHigh: Starts at the lowest index (e.g., 0) and progresses upwards through the tiers.
/// • HighToLow: Starts at the highest index (e.g., N-1) and progresses downwards through the tiers.
/// </summary>
public enum CacheDirection
{
    /// <summary>
    /// Represents a cache behavior where operations are performed in ascending order of tiers,
    /// starting from the lowest index (0) and proceeding sequentially to the highest index (N-1).
    /// </summary>
    LowToHigh,

    /// <summary>
    /// Represents a cache behavior where operations are performed in descending order of tiers,
    /// starting from the highest index (N-1) and proceeding sequentially to the lowest index (0).
    /// </summary>
    HighToLow
}