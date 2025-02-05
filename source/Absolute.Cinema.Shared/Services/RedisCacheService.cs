using System.Text.Json;
using Absolute.Cinema.Shared.Interfaces;
using StackExchange.Redis;

namespace Absolute.Cinema.Shared.Services;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, int dbIndex = 0)
    {
        var cache = redis.GetDatabase(dbIndex);
        var value = await cache.StringGetAsync(key);
        return value.IsNull ? default : JsonSerializer.Deserialize<T>(value!);
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, int dbIndex = 0)
    {
        var cache = redis.GetDatabase(dbIndex);
        var json = JsonSerializer.Serialize(value);
        return await cache.StringSetAsync(key, json, expiry);
    }

    public async Task<bool> DeleteAsync(string key, int dbIndex = 0)
    {
        var cache = redis.GetDatabase(dbIndex);
        return await cache.KeyDeleteAsync(key);
    }

    public async Task<T?> GetDeleteAsync<T>(string key, int dbIndex = 0)
    {
        var cache = redis.GetDatabase(dbIndex);
        var value = await cache.StringGetDeleteAsync(key);
        return value.IsNull ? default : JsonSerializer.Deserialize<T>(value!);
    }

    public async Task<bool> ExistsAsync(string key, int dbIndex = 0)
    {
        var cache = redis.GetDatabase(dbIndex);
        return await cache.KeyExistsAsync(key);
    }

    public bool IsConnected(int dbIndex = 0)
    {
        if (!redis.IsConnected)
            return false;

        var cache = redis.GetDatabase(dbIndex);
        return cache.Ping().TotalMilliseconds > 0;
    }
}