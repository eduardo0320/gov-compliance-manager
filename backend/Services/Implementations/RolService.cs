using backend.Models;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;

namespace backend.Services.Implementations
{
    public class RolService : IRolService
    {
        private readonly IRolRepository _rolRepository;


        public RolService(
            IRolRepository rolRepository)
        {
            _rolRepository = rolRepository;
        }

        // Operaciones CRUD
        public async Task<IEnumerable<Rol>> ObtenerTodosLosRoles()
        {
            return await _rolRepository.ObtenerTodos();
        }

        public async Task<Rol?> ObtenerRolPorId(int id)
        {
            return await _rolRepository.ObtenerPorId(id);
        }

        public async Task<Rol?> ObtenerRolPorNombre(string nombre)
        {
            return await _rolRepository.BuscarPorNombre(nombre);
        }

        public async Task<Rol> CrearRolAsync(Rol rol)
        {
            // Validaciones de negocio
            if (await ExisteRolPorNombreAsync(rol.nombre))
                throw new InvalidOperationException($"Ya existe un rol con el nombre: {rol.nombre}");

            var resultado = await _rolRepository.Agregar(rol);
            await _rolRepository.GuardarCambios();
            return resultado;
        }

        public async Task<bool> ActualizarRolAsync(Rol rol)
        {
            var rolExistente = await _rolRepository.ObtenerPorId(rol.idRol);
            if (rolExistente == null) return false;

            // Validar que el nombre del rol no esté en uso por otro rol
            if (rolExistente.nombre != rol.nombre &&
                await ExisteRolPorNombreAsync(rol.nombre))
                throw new InvalidOperationException($"Ya existe un rol con el nombre: {rol.nombre}");

            await _rolRepository.Actualizar(rol);
            await _rolRepository.GuardarCambios();
            return true;
        }

        // public async Task<bool> EliminarRolAsync(int id) // hay que revisarlo
        // {
        //     // Validar que no haya usuarios asignados a este rol
        //     if (!await PuedeEliminarRolAsync(id))
        //         throw new InvalidOperationException("No se puede eliminar el rol porque tiene usuarios asignados");

        //     await _rolRepository.Eliminar(id);
        //     await _rolRepository.GuardarCambios();
        //     return true;
        // }

        // Operaciones de negocio específicas
        public async Task<bool> ExisteRolPorNombreAsync(string nombreRol)
        {
            var rol = await _rolRepository.BuscarPorNombre(nombreRol);
            return rol != null;
        }


        public async Task<bool> ValidarPermisoRolAsync(string nombreRol, string permisoRequerido)
        {
            var rol = await _rolRepository.BuscarPorNombre(nombreRol);
            if (rol == null) return false;

            // Implementar lógica de permisos específica
            // Por ahora, SUPERADMIN tiene todos los permisos, ADMIN tiene permisos limitados
            if (rol.nombre == "SUPERADMIN") return true;
            if (rol.nombre == "ADMIN" && permisoRequerido != "MANAGE_ADMINS") return true;

            return false;
        }

        // Operaciones de búsqueda
        public async Task<IEnumerable<Rol>> BuscarRolesPorNombreAsync(string nombre)
        {
            var todosLosRoles = await _rolRepository.ObtenerTodos();
            return todosLosRoles.Where(r => r.nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));
        }
        public async Task<bool> validarRolesExistentes()
        {
            var rolesNecesarios = new List<string> { "SUPERADMIN", "ADMIN", "EDITOR" };
            foreach (var nombreRol in rolesNecesarios)
            {
                if (!await ExisteRolPorNombreAsync(nombreRol))
                {
                    var nuevoRol = new Rol { nombre = nombreRol };
                    await _rolRepository.Agregar(nuevoRol);
                    Console.WriteLine($"👤 Creando rol faltante: {nombreRol}");
                }
            }

            await _rolRepository.GuardarCambios();
            return true;
        }
    }
}
