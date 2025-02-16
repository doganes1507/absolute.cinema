using Absolute.Cinema.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Absolute.Cinema.Shared.Services;

public class CachedRepository : ICachedRepository
{
    private readonly DbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly TimeSpan? _defaultExpiry;

    public CachedRepository(DbContext dbContext, ICacheService cacheService, CachedRepositoryOptions options)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _defaultExpiry = options.DefaultExpiry;
    }

    public async Task<T?> ReadAsync<T>(Func<Task<T?>> dbFetch, string key, TimeSpan? expiry = null, int dbIndex = 0)
        where T : class
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
            await _cacheService.SetAsync(key, entity, expiry ?? _defaultExpiry, dbIndex);
        }

        return entity;
    }

    public async Task WriteAsync<T>(T entity, string key, TimeSpan? expiry = null, int dbIndex = 0) where T : class
    {
        await _dbContext.Set<T>().AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        if (_cacheService.IsConnected())
        {
            await _cacheService.SetAsync(key, entity, expiry ?? _defaultExpiry, dbIndex);
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

public static class CachedRepositoryExtensions
{
    public static IServiceCollection AddCachedRepository(this IServiceCollection services,
        Action<CachedRepositoryOptions>? optionsDelegate = null)
    {
        var options = new CachedRepositoryOptions();
        optionsDelegate?.Invoke(options);

        services.AddScoped<ICachedRepository>(provider => new CachedRepository(
            provider.GetRequiredService<DbContext>(),
            provider.GetRequiredService<ICacheService>(),
            options
        ));

        return services;
    }
}

public class CachedRepositoryOptions
{
    public TimeSpan? DefaultExpiry { get; private set; }

    public CachedRepositoryOptions WithDefaultExpiry(TimeSpan? expiry = null)
    {
        DefaultExpiry = expiry;
        return this;
    }
}