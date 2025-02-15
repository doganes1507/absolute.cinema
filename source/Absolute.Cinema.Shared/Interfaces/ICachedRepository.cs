namespace Absolute.Cinema.Shared.Interfaces;

public interface ICachedRepository
{
    public Task<T?> ReadAsync<T>(Func<Task<T?>> dbFetch, string key, TimeSpan? expiry = null, int dbIndex = 0) where T : class;
    public Task WriteAsync<T>(T entity, string key, TimeSpan? expiry = null, int dbIndex = 0) where T : class;
    public Task RemoveAsync<T>(Func<Task<T?>> dbFetch, string key, int dbIndex = 0) where T : class;
}