namespace Absolute.Cinema.Shared.Interfaces;

public interface ICacheAsideService
{
    public Task<T?> ReadAsync<T>(Func<Task<T?>> dbFetchFunc, string cacheKey, TimeSpan? expiry = null, int dbIndex = 0)
        where T : class;

    public Task CreateAsync<T>(T entity, string cacheKey, TimeSpan? expiry = null, int dbIndex = 0) where T : class;

    public Task<T?> WriteAsync<T>(Func<Task<T?>> dbWriteFunc, string cacheKey, TimeSpan? expiry = null, int dbIndex = 0)
        where T : class;

    public Task RemoveAsync<T>(Func<Task<T?>> dbFetchFunc, string cacheKey, int dbIndex = 0) where T : class;
}