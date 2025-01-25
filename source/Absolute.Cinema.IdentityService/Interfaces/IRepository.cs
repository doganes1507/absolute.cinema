using System.Linq.Expressions;

namespace Absolute.Cinema.IdentityService.Interfaces;

public interface IRepository<T>
{
    public Task<T?> Find(Expression<Func<T, bool>> predicate);
    public Task<ICollection<T>> GetAll();
    public Task<T?> GetById(Guid id);
    public Task Create(T entity);
    public Task Update(T entity);
    public Task Remove(T entity);
}