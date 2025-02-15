using Absolute.Cinema.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.Shared.Services;

public class CachedRepository : ICachedRepository
{
    private readonly DbContext _dbContext;
    private readonly ICacheService _cacheService;
    
    public CachedRepository(DbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }
    
    public async Task<T?> ReadAsync<T>(Func<Task<T?>> dbFetch, string key, TimeSpan? expiry = null, int dbIndex = 0) where T : class
    {
        if (_cacheService.IsConnected())
        {
            var cachedEntity = await _cacheService.GetAsync<T>(key, dbIndex);
            if (cachedEntity is not null)
            {
                return cachedEntity;
            }
        }

        var entity = await dbFetch();
    
        if (entity is not null && _cacheService.IsConnected())
        {
            await _cacheService.SetAsync(key, entity, expiry, dbIndex);
        }
    
        return entity;
    }

    public async Task WriteAsync<T>(T entity, string key, TimeSpan? expiry = null, int dbIndex = 0) where T : class
    {
        await _dbContext.Set<T>().AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        if (_cacheService.IsConnected())
        {
            await _cacheService.SetAsync(key, entity, expiry, dbIndex);
        }
    }

    public async Task RemoveAsync<T>(Func<Task<T?>> dbFetch, string key, int dbIndex = 0) where T : class
    {
        var entity = await dbFetch();
        if (entity is not null)
        {
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        if (_cacheService.IsConnected())
        {
            await _cacheService.DeleteAsync(key, dbIndex);
        }
    }
}