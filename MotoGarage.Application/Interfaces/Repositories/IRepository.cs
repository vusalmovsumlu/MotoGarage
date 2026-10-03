using System.Linq.Expressions;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();

    Task<T?> GetByIdAsync(int id);

    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate);

    Task AddAsync(T entity);

    Task UpdateAsync(T entity);

    Task DeleteAsync(T entity);

    Task SaveChangesAsync();
}