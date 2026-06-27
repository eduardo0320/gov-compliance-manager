using System.Linq.Expressions;

namespace backend.Repositories.Interfaces
{
    public interface IRepository<T, TKey> where T : class
    {
        // Operaciones básicas CRUD
        Task<T?> ObtenerPorId(TKey id);
        Task<IEnumerable<T>> ObtenerTodos();
        Task<T> Agregar(T entity);
        Task<T> Actualizar(T entity);
        Task<bool> Eliminar(TKey id);
        Task<bool> Existe(TKey id);

        // Operaciones de búsqueda avanzada
        Task<IEnumerable<T>> BuscarAsync(Expression<Func<T, bool>> predicate);
        Task<T?> PrimeroODefaultAsync(Expression<Func<T, bool>> predicate);

        // Operaciones con paginación
        Task<(IEnumerable<T> Items, int TotalCount)> ObtenerPaginadoAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null
        );

        // Conteo
        Task<int> Contar(Expression<Func<T, bool>>? predicate = null);

        // Guardar cambios
        Task<int> GuardarCambios();

    }
}
