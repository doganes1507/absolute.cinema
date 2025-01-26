using System.Linq.Expressions;

namespace Absolute.Cinema.IdentityService.Interfaces;

public interface IRepository<T>
{
    public Task<T?> FindAsync(Expression<Func<T, bool>> predicate);
    public Task<ICollection<T>> FindAllAsync(Expression<Func<T, bool>> predicate);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    public Task<ICollection<T>> GetAllAsync();
    public Task<T?> GetByIdAsync(Guid id);
    public Task CreateAsync(T entity);
    public Task UpdateAsync(T entity);
    public Task RemoveAsync(T entity);
}