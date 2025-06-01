namespace DomsUtils.Services.Caching.Interfaces.Bases;

/// <summary>
/// Interface defining methods for checking cache system availability.
/// </summary>
public interface ICacheAvailability
{
    /// <summary>
    /// Checks whether the cache is available for use.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the cache is operational and available.
    /// Returns true if the cache is available, otherwise false.
    /// </returns>
    bool IsAvailable();
}