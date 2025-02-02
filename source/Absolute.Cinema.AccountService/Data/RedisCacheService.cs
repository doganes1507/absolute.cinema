using System.Text.Json;
using StackExchange.Redis;

namespace Absolute.Cinema.AccountService.Data;

public class RedisCacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key, int dbIndex = 0)
    {
        var cache = _redis.GetDatabase(dbIndex);
        var value = await cache.StringGetAsync(key);
        return value.IsNull ? default : JsonSerializer.Deserialize<T>(value!);
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, int dbIndex = 0)
    {
        var cache = _redis.GetDatabase(dbIndex);
        var json = JsonSerializer.Serialize(value);
        return await cache.StringSetAsync(key, json, expiry);
    }

    public async Task<bool> RemoveAsync(string key, int dbIndex = 0)
    {
        var cache = _redis.GetDatabase(dbIndex);
        return await cache.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key, int dbIndex = 0)
    {
        var cache = _redis.GetDatabase(dbIndex);
        return await cache.KeyExistsAsync(key);
    }

    public bool IsConnected(int dbIndex = 0)
    {
        if (!_redis.IsConnected)
            return false;

        var cache = _redis.GetDatabase(dbIndex);
        return cache.Ping().TotalMilliseconds > 0;
    }
}