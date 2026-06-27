using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IRolRepository : IRepository<Rol, int>
    {

        Task<Rol?> BuscarPorNombre(string nombre);
        Task<bool> ExistePorNombre(string nombre);
        Task<IEnumerable<Usuario>> ObtenerUsuariosPorIdRol(int rolId);

        // Búsquedas con paginación
        Task<(IEnumerable<Rol> Items, int TotalCount)> BuscarPorNombreConteniendo(
            string nombre, int page, int pageSize);
    }
}
