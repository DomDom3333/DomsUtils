namespace DomsUtils.Services.Pipeline.BlockStorage;

/// <summary>
/// Provides block modifiers that interact with pipeline storage.
/// These modifiers allow blocks to read from and write to shared storage.
/// </summary>
public static class StorageBlockModifiers
{
    /// <summary>
    /// Creates a modifier that stores the result of processing in the pipeline storage.
    /// </summary>
    /// <typeparam name="T">The type of data being processed.</typeparam>
    /// <typeparam name="TKey">The type of keys used for storage.</typeparam>
    /// <param name="pipeline">The pipeline or object containing the storage.</param>
    /// <param name="keySelector">Function to derive a storage key from the processed item.</param>
    /// <param name="storageName">Optional name of the storage to use. Defaults to "default".</param>
    /// <returns>A block modifier that stores processing results.</returns>
    public static BlockModifier<T> StoreResult<T, TKey>(
        object pipeline,
        Func<T, TKey> keySelector,
        string storageName = "default") 
        where TKey : notnull
    {
        return next => async (env, ct) =>
        {
            var result = await next(env, ct).ConfigureAwait(false);
            var storage = pipeline.GetStorage<TKey, T>(storageName);
            if (storage != null)
            {
                storage.SetValue(keySelector(result.Value), result.Value);
            }
            return result;
        };
    }

    /// <summary>
    /// Creates a modifier that stores a custom value in the pipeline storage based on the processing result.
    /// </summary>
    /// <typeparam name="T">The type of data being processed.</typeparam>
    /// <typeparam name="TKey">The type of keys used for storage.</typeparam>
    /// <typeparam name="TValue">The type of values to store.</typeparam>
    /// <param name="pipeline">The pipeline or object containing the storage.</param>
    /// <param name="keySelector">Function to derive a storage key from the processed item.</param>
    /// <param name="valueSelector">Function to derive a value to store from the processed item.</param>
    /// <param name="storageName">Optional name of the storage to use. Defaults to "default".</param>
    /// <returns>A block modifier that stores custom values derived from processing results.</returns>
    public static BlockModifier<T> StoreValue<T, TKey, TValue>(
        object pipeline,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector,
        string storageName = "default") 
        where TKey : notnull
    {
        return next => async (env, ct) =>
        {
            var result = await next(env, ct).ConfigureAwait(false);
            var storage = pipeline.GetStorage<TKey, TValue>(storageName);
            if (storage != null)
            {
                storage.SetValue(keySelector(result.Value), valueSelector(result.Value));
            }
            return result;
        };
    }

    /// <summary>
    /// Creates a modifier that can retrieve data from storage during processing.
    /// </summary>
    /// <typeparam name="T">The type of data being processed.</typeparam>
    /// <typeparam name="TKey">The type of keys used for storage.</typeparam>
    /// <typeparam name="TValue">The type of values stored in storage.</typeparam>
    /// <param name="pipeline">The pipeline or object containing the storage.</param>
    /// <param name="transformer">Function that uses storage to transform the processed item.</param>
    /// <param name="storageName">Optional name of the storage to use. Defaults to "default".</param>
    /// <returns>A block modifier that enhances processing with stored data.</returns>
    public static BlockModifier<T> WithStoredData<T, TKey, TValue>(
        object pipeline,
        Func<T, IBlockStorage<TKey, TValue>?, T> transformer,
        string storageName = "default") 
        where TKey : notnull
    {
        return next => async (env, ct) =>
        {
            // Get storage at execution time
            var storage = pipeline.GetStorage<TKey, TValue>(storageName);

            // Apply the transformer before passing to the next block
            var enhancedValue = transformer(env.Value, storage);
            var enhancedEnv = new Envelope<T>(env.Index, enhancedValue);

            return await next(enhancedEnv, ct).ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Creates a modifier that skips processing if a condition based on stored data is met.
    /// </summary>
    /// <typeparam name="T">The type of data being processed.</typeparam>
    /// <typeparam name="TKey">The type of keys used for storage.</typeparam>
    /// <typeparam name="TValue">The type of values stored in storage.</typeparam>
    /// <param name="pipeline">The pipeline or object containing the storage.</param>
    /// <param name="keySelector">Function to derive a storage key from the processed item.</param>
    /// <param name="skipPredicate">Predicate that determines if processing should be skipped based on stored value.</param>
    /// <param name="storageName">Optional name of the storage to use. Defaults to "default".</param>
    /// <returns>A block modifier that conditionally skips processing.</returns>
    public static BlockModifier<T> SkipIfStored<T, TKey, TValue>(
        object pipeline,
        Func<T, TKey> keySelector,
        Func<TValue?, bool> skipPredicate,
        string storageName = "default") 
        where TKey : notnull
    {
        return next => async (env, ct) =>
        {
            // Get storage at execution time
            var storage = pipeline.GetStorage<TKey, TValue>(storageName);

            // If no storage is available, we can't skip based on storage
            if (storage != null)
            {
                var key = keySelector(env.Value);
                storage.TryGetValue(key, out TValue? storedValue);

                if (skipPredicate(storedValue))
                {
                    // Skip processing and return the envelope as is
                    return env;
                }
            }

            return await next(env, ct).ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Creates a modifier that stores an error encountered during processing.
    /// </summary>
    /// <typeparam name="T">The type of data being processed.</typeparam>
    /// <typeparam name="TKey">The type of keys used for storage.</typeparam>
    /// <param name="pipeline">The pipeline or object containing the storage.</param>
    /// <param name="keySelector">Function to derive a storage key from the processed item.</param>
    /// <param name="storageName">Optional name of the storage to use. Defaults to "errors".</param>
    /// <returns>A block modifier that stores errors and rethrows them.</returns>
    public static BlockModifier<T> CaptureErrors<T, TKey>(
        object pipeline,
        Func<T, TKey> keySelector,
        string storageName = "errors") 
        where TKey : notnull
    {
        return next => async (env, ct) =>
        {
            try
            {
                return await next(env, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var storage = pipeline.GetStorage<TKey, Exception>(storageName);
                if (storage != null)
                {
                    storage.SetValue(keySelector(env.Value), ex);
                }
                throw; // Rethrow to maintain pipeline error handling
            }
        };
    }
}
