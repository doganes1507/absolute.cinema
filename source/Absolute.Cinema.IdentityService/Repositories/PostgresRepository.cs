using Absolute.Cinema.IdentityService.Data;
using Absolute.Cinema.IdentityService.Interfaces;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.IdentityService.Repositories;

public class PostgresRepository<T> : IRepository<T> where T : class
{
    public PostgresRepository (DatabaseContext context)
    {
        _context = context;
    }
    
    private readonly DatabaseContext _context;
    
    public async Task<T?> Find(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().FirstOrDefaultAsync(predicate);
    }
     
    public async Task<ICollection<T>> GetAll()
    {
        return await _context.Set<T>().ToArrayAsync();
    }
 
    public async Task<T?> GetById(Guid id)
    {
        return await _context.Set<T>().FindAsync(id);
    }
 
    public async Task Create(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }
 
    public async Task Update(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }
 
    public async Task Remove(T entity)
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
        
    }
}
