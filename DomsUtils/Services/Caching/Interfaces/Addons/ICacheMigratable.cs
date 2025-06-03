namespace DomsUtils.Services.Caching.Interfaces.Addons;

/// <summary>
/// Defines an interface for cache migration operations to allow triggering migration processes.
/// Implementers of this interface provide functionality to migrate cache data when requested.
/// </summary>
public interface ICacheMigratable
{
    /// <summary>
    /// Initiates the migration process for cache data.
    /// </summary>
    /// <remarks>
    /// This method triggers the immediate migration of data for the current cache
    /// implementation. It is typically used in scenarios where data needs to be
    /// moved from one cache storage to another or when transitioning between
    /// different caching strategies.
    /// </remarks>
    void TriggerMigrationNow();
}