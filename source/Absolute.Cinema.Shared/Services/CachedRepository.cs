using Absolute.Cinema.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Absolute.Cinema.Shared.Services;

public class CachedRepository<TContext> : ICachedRepository where TContext : DbContext
{
    private readonly TContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly TimeSpan? _defaultExpiry;

    public CachedRepository(TContext dbContext, ICacheService cacheService, CachedRepositoryOptions options)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _defaultExpiry = options.DefaultExpiry;
    }

    public async Task<T?> ReadAsync<T>(Func<Task<T?>> dbFetchFunc, string cacheKey, TimeSpan? expiry = null, int dbIndex = 0)
        where T : class
    {
        if (_cacheService.IsConnected())
        {
            var cachedEntity = await _cacheService.GetAsync<T>(cacheKey, dbIndex);
            if (cachedEntity is not null)
            {
                return cachedEntity;
            }
        }

        var entity = await dbFetchFunc();

        if (entity is not null && _cacheService.IsConnected())
        {
            await _cacheService.SetAsync(cacheKey, entity, expiry ?? _defaultExpiry, dbIndex);
        }

        return entity;
    }

    public async Task<T?> WriteAsync<T>(Func<Task<T?>> dbWriteFunc, string cacheKey, TimeSpan? expiry = null, int dbIndex = 0)
        where T : class
    {
        var entity = await dbWriteFunc();

        if (entity is not null && _cacheService.IsConnected())
            await _cacheService.SetAsync(cacheKey, entity, expiry ?? _defaultExpiry, dbIndex);

        return entity;
    }
    
    public async Task CreateAsync<T>(T entity, string cacheKey, TimeSpan? expiry = null, int dbIndex = 0) where T : class
    {
        await _dbContext.Set<T>().AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        if (_cacheService.IsConnected())
        {
            await _cacheService.SetAsync(cacheKey, entity, expiry ?? _defaultExpiry, dbIndex);
        }
    }

    public async Task RemoveAsync<T>(Func<Task<T?>> dbFetchFunc, string cacheKey, int dbIndex = 0) where T : class
    {
        var entity = await dbFetchFunc();
        if (entity is not null)
        {
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        if (_cacheService.IsConnected())
        {
            await _cacheService.DeleteAsync(cacheKey, dbIndex);
        }
    }
}

public static class CachedRepositoryExtensions
{
    public static IServiceCollection AddCachedRepository<TContext>(this IServiceCollection services,
        Action<CachedRepositoryOptions>? optionsDelegate = null) where TContext : DbContext
    {
        var options = new CachedRepositoryOptions();
        optionsDelegate?.Invoke(options);

        services.AddScoped<ICachedRepository>(provider => new CachedRepository<TContext>(
            provider.GetRequiredService<TContext>(),
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