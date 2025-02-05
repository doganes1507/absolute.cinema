namespace Absolute.Cinema.AccountService.Interfaces;

public interface ICacheService
{
    public Task<T?> GetAsync<T>(string key, int dbIndex = 0);
    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, int dbIndex = 0);
    public Task<bool> DeleteAsync(string key, int dbIndex = 0);
    public Task<T?> GetDeleteAsync<T>(string key, int dbIndex = 0);
    public Task<bool> ExistsAsync(string key, int dbIndex = 0);
    public bool IsConnected(int dbIndex = 0);
}