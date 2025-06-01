    using DomsUtils.Services.Caching.Interfaces.Bases;

    namespace DomsUtils.Services.Caching.Hybrids;

    /// <summary>
    /// Represents a hybrid cache that performs read and write operations across multiple caches in parallel.
    /// The cache attempts to retrieve data from the first available cache in priority order.
    /// Implements ICache for standard cache operations and ICacheAvailability to monitor availability status.
    /// </summary>
    public class ParallelCache<TKey, TValue> : ICache<TKey, TValue>, ICacheAvailability
    {
        /// <summary>
        /// A collection of underlying cache implementations that the ParallelCache works with.
        /// Used to manage multiple cache instances for read/write and availability operations.
        /// </summary>
        private readonly IList<ICache<TKey, TValue>> _caches;

        /// <summary>
        /// Hybrid cache that writes to all underlying caches in parallel, and reads from them in priority order.
        /// Implements ICache so it can be nested.
        /// </summary>
        public ParallelCache(params ICache<TKey, TValue>[] caches)
        {
            if (caches == null || caches.Length < 2)
                throw new ArgumentException("At least two caches are required for a ParallelCache.");

            _caches = new List<ICache<TKey, TValue>>(caches);
        }

        /// <summary>
        /// Attempts to retrieve the value associated with the specified key from the first available cache in order.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key if found, or the default value for the type if not found.</param>
        /// <returns>True if the value is found in any cache; otherwise, false.</returns>
        public bool TryGet(TKey key, out TValue value)
        {
            foreach (var cache in _caches)
            {
                if (cache is ICacheAvailability avail && !avail.IsAvailable())
                    continue;
                try
                {
                    if (cache.TryGet(key, out value))
                        return true;
                }
                catch
                {
                    // Skip failing cache
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Writes a key-value pair to all available underlying caches in parallel.
        /// </summary>
        /// <param name="key">The key associated with the value to be cached.</param>
        /// <param name="value">The value to be cached.</param>
        public void Set(TKey key, TValue value)
        {
            foreach (var cache in _caches)
            {
                if (cache is ICacheAvailability avail && !avail.IsAvailable())
                    continue;
                try { cache.Set(key, value); } catch { }
            }
        }

        /// <summary>
        /// Removes the specified key from all available caches.
        /// </summary>
        /// <param name="key">The key to remove from the caches.</param>
        /// <returns>
        /// A boolean value indicating whether the key was successfully removed from at least one cache.
        /// </returns>
        public bool Remove(TKey key)
        {
            bool removed = false;
            foreach (var cache in _caches)
            {
                if (cache is ICacheAvailability avail && !avail.IsAvailable())
                    continue;
                try { removed |= cache.Remove(key); } catch { }
            }
            return removed;
        }

        /// <summary>
        /// Clears the contents of all underlying caches that are currently available.
        /// </summary>
        public void Clear()
        {
            foreach (var cache in _caches)
            {
                if (cache is ICacheAvailability avail && !avail.IsAvailable())
                    continue;
                try { cache.Clear(); } catch { }
            }
        }

        /// <summary>
        /// Determines whether at least one underlying cache is available for use.
        /// </summary>
        /// <returns>
        /// A boolean value indicating whether the cache system is operational and available.
        /// Returns true if any underlying cache is available, otherwise false.
        /// </returns>
        public bool IsAvailable()
        {
            // Consider available if at least one underlying cache is available
            return _caches.Any(c => !(c is ICacheAvailability avail) || avail.IsAvailable());
        }
    }