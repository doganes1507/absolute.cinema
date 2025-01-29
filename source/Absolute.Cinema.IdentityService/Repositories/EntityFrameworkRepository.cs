using Absolute.Cinema.IdentityService.Data;
using Absolute.Cinema.IdentityService.Interfaces;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.IdentityService.Repositories;

public class EntityFrameworkRepository<T> : IRepository<T> where T : class
{
    public EntityFrameworkRepository (DatabaseContext context)
    {
        _context = context;
    }
    
    private readonly DatabaseContext _context;
    
    public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().FirstOrDefaultAsync(predicate);
    }

    public async Task<ICollection<T>> FindAllAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().Where(predicate).ToListAsync();
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().AnyAsync(predicate);;
    }

    public async Task<ICollection<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToArrayAsync();
    }
 
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _context.Set<T>().FindAsync(id);
    }
 
    public async Task CreateAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }
 
    public async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }
 
    public async Task RemoveAsync(T entity)
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
        
    }
}
