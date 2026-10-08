using System.Linq.Expressions;

namespace ConnectHub.DAL.Interfaces
{
    /// <summary>
    /// Defines generic data-access operations common across all entities.
    /// </summary>
    public interface IGenericRepository<T> where T : class
    {
            IQueryable<T> Query();
            Task<T?> GetByIdAsync(Guid id);
            Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
            Task AddAsync(T entity);
            Task AddRangeAsync(IEnumerable<T> entities);
            void Update(T entity);
            void UpdateRange(IEnumerable<T> entities);
            void Delete(T entity);
            void DeleteRange(IEnumerable<T> entities);
        
    }
}
