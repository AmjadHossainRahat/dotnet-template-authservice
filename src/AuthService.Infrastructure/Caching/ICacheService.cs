namespace AuthService.Infrastructure.Caching
{
    public interface ICacheService
    {
        /// <summary>
        /// Stores a value in the cache.
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

        /// <summary>
        /// Retrieves a cached value.
        /// </summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Removes an item from the cache.
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Checks if a key exists.
        /// </summary>
        Task<bool> ExistsAsync(string key);
    }
}
