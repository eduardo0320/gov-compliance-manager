using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class UsuarioRepository : Repository<Usuario, int>, IUsuarioRepository
    {
        public UsuarioRepository(NormasDb context) : base(context)
        {
        }

        public async Task<Usuario?> EncontrarPorCedula(string cedula)
        {
            return await _dbSet
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.cedula == cedula);
        }

        public async Task<Usuario?> EncontrarPorCorreoElectronico(string correoElectronico)
        {
            return await _dbSet
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.correo_electronico == correoElectronico);
        }

        public async Task<IEnumerable<Usuario>> EncontrarPorDepartamento(string departamento)
        {
            return await _dbSet
                .Include(u => u.Rol)
                .Where(u => u.departamento != null && u.departamento.Contains(departamento))
                .ToListAsync();
        }

        public async Task<IEnumerable<Usuario>> EncontrarPorIdRol(int rolId)
        {
            return await _dbSet
                .Include(u => u.Rol)
                .Where(u => u.idRol == rolId)
                .ToListAsync();
        }

        public async Task<bool> ExistePorCedula(string cedula)
        {
            return await _dbSet.AnyAsync(u => u.cedula == cedula);
        }

        public async Task<bool> ExistePorCorreoElectronico(string correoElectronico)
        {
            return await _dbSet.AnyAsync(u => u.correo_electronico == correoElectronico);
        }

        public async Task<(IEnumerable<Usuario> Items, int TotalCount)> BuscarPorNombreConteniendo(
            string nombre, int page, int pageSize)
        {
            var query = _dbSet
                .Include(u => u.Rol)
                .Where(u => u.nombre.Contains(nombre));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Usuario> Items, int TotalCount)> EncontrarPorDepartamentoConteniendo(
            string departamento, int page, int pageSize)
        {
            var query = _dbSet
                .Include(u => u.Rol)
                .Where(u => u.departamento != null && u.departamento.Contains(departamento));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Override: sobreescribe funcion padre heredada
        //  para incluir Rol al obtener
        public override async Task<Usuario?> ObtenerPorId(int id)
        {
            return await _dbSet
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id_Usuario == id);
        }

        // Override: sobreescribe funcion padre heredada
        //  para incluir Rol al obtener
        public override async Task<IEnumerable<Usuario>> ObtenerTodos()
        {
            return await _dbSet
                .Include(u => u.Rol)
                .ToListAsync();
        }

        // Métodos específicos para edición de perfil propio (HU-005)
        public async Task<bool> ExistePorCorreoElectronicoExceptoUsuarioAsync(string correoElectronico, int userId)
        {
            return await _dbSet.AnyAsync(u => u.correo_electronico == correoElectronico && u.Id_Usuario != userId);
        }

        public async Task<bool> ActualizarMiPerfilAsync(int userId, string nombre, string correoElectronico, string? departamento)
        {
            var usuario = await _dbSet.FindAsync(userId);
            if (usuario == null)
                return false;

            // Solo actualizar campos permitidos para edición de perfil propio
            usuario.nombre = nombre;
            usuario.correo_electronico = correoElectronico;
            usuario.departamento = departamento;
            usuario.fechaUltimaModificacion = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarintentosLoginFallidosAsync(string cedula, int intentos)
        {
            var usuario = await EncontrarPorCedula(cedula);
            if (usuario != null)
            {
                usuario.intentosLoginFallidos = intentos;
                await GuardarCambios();
                return true;
            }
            return false;
        }

        public async Task<bool> ActualizarFechaBloqueadoAsync(string cedula, DateTime fecha)
        {
            var usuario = await EncontrarPorCedula(cedula);
            if (usuario != null)
            {
                usuario.fechaBloqueado = fecha;
                await GuardarCambios();
                return true;
            }
            return false;
        }


    }
}
