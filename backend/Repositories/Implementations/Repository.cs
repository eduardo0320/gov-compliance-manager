using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using backend.Data;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class Repository<T, TKey> : IRepository<T, TKey> where T : class
    {
        protected readonly NormasDb _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(NormasDb context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> ObtenerPorId(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> ObtenerTodos()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T> Agregar(T entity)
        {
            var result = await _dbSet.AddAsync(entity);
            return result.Entity;
        }

        public virtual Task<T> Actualizar(T entity)
        {
            _dbSet.Update(entity);
            return Task.FromResult(entity);
        }

        public virtual async Task<bool> Eliminar(TKey id)
        {
            var entity = await ObtenerPorId(id);
            if (entity == null)
                return false;

            _dbSet.Remove(entity);
            return true;
        }

        public virtual async Task<bool> Existe(TKey id)
        {
            var entity = await ObtenerPorId(id);
            return entity != null;
        }

        public virtual async Task<IEnumerable<T>> BuscarAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> PrimeroODefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> ObtenerPaginadoAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var totalCount = await query.CountAsync();

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public virtual async Task<int> Contar(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();

            return await _dbSet.CountAsync(predicate);
        }

        public virtual async Task<int> GuardarCambios()
        {
            return await _context.SaveChangesAsync();
        }

    }
}
