using backend.Models;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;

namespace backend.Services.Interfaces
{
    public interface IRolService
    {
        // Operaciones CRUD básicas
        Task<IEnumerable<Rol>> ObtenerTodosLosRoles();
        Task<Rol?> ObtenerRolPorId(int id);
        Task<Rol?> ObtenerRolPorNombre(string nombre);
        Task<Rol> CrearRolAsync(Rol rol);
        Task<bool> ActualizarRolAsync(Rol rol);
        Task<bool> ExisteRolPorNombreAsync(string nombre);
        Task<bool> ValidarPermisoRolAsync(string rolActual, string accionRequerida);

        // Task<bool> EliminarRolAsync(int id); TOCA REVISARLA
        // Task<bool> PuedeEliminarRolAsync(int rolId); TOCA REVISARLA
        Task<bool> validarRolesExistentes();
    }
}
