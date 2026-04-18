using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GameShelf.Business.Repositories.Interfaces
{
    /// <summary>
    /// Defines a generic repository interface for common data access operations
    /// </summary>
    /// <typeparam name="T">The type of the entity</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Retrives single entity by its GUID
        /// </summary>
        /// <param name="id">The unique identifier of the entity</param>
        /// <param name="includes">Optional navigation properties to include</param>
        /// <returns>The entity if found; otherwise, null</returns>
        Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Retrieves all entities
        /// </summary>
        /// <param name="includes">Optional navigation properties to include</param>
        /// <returns>A list of all entities</returns>
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Finds all entities matching the specified predicate
        /// </summary>
        /// <param name="predicate">The filtering condition</param>
        /// <param name="includes">Optional navigation properties to include</param>
        /// <returns>A list of matching entities</returns>
        Task<IEnumerable<T>> FilterAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Add a new entity to the repository
        /// </summary>
        /// <param name="entity">The entity to add</param>
        Task AddAsync(T entity);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        void Update(T entity);

        /// <summary>
        /// Removes an entity from the repository
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        void Remove(T entity);

        /// <summary>
        /// Commits all changes made in the current unit of work
        /// </summary>
        /// <returns>The number of affected rows</returns>
        Task<int> CommitAsync();

        /// <summary>
        /// Provides a queryable interface for the repository, allowing for LINQ queries
        /// </summary>
        /// <returns>An <see cref="IQueryable{T}"/> for entity type.</returns>
        public IQueryable<T> Query();
    }
}
