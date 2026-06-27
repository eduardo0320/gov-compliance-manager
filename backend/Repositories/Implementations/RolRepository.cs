using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class RolRepository : Repository<Rol, int>, IRolRepository
    {
        public RolRepository(NormasDb context) : base(context)
        {
        }

        public async Task<Rol?> BuscarPorNombre(string nombre)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.nombre == nombre);
        }

        public async Task<bool> ExistePorNombre(string nombre)
        {
            return await _dbSet.AnyAsync(r => r.nombre == nombre);
        }

        public async Task<IEnumerable<Usuario>> ObtenerUsuariosPorIdRol(int rolId)
        {
            return await _context.Usuarios
                .Where(u => u.idRol == rolId)
                .Include(u => u.Rol)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Rol> Items, int TotalCount)> BuscarPorNombreConteniendo(
            string nombre, int page, int pageSize)
        {
            var query = _dbSet.Where(r => r.nombre.Contains(nombre));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }



    }
}
