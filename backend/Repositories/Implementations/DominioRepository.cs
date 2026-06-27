using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class DominioRepository : Repository<Dominio, int>, IDominioRepository
    {
        public DominioRepository(NormasDb context) : base(context)
        {
        }

        public async Task<Dominio?> FindByNombreAsync(string nombre)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.Nombre == nombre);
        }

        public async Task<bool> ExistsByNombreAsync(string nombre)
        {
            return await _dbSet.AnyAsync(d => d.Nombre == nombre);
        }

        public async Task<IEnumerable<Proceso>> GetProcesosByDominioIdAsync(int dominioId)
        {
            return await _context.Procesos
                .Where(p => p.DominioId == dominioId)
                .Include(p => p.Dominio)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Dominio> Items, int TotalCount)> FindByNombreContainingAsync(
            string nombre, int page, int pageSize)
        {
            var query = _dbSet.Where(d => d.Nombre.Contains(nombre));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Dominio?> GetByIdWithProcesosAsync(int id)
        {
            return await _dbSet
                .Include(d => d.Procesos)
                .FirstOrDefaultAsync(d => d.IdDominio == id);
        }

        public async Task<IEnumerable<Dominio>> GetAllWithProcesosAsync()
        {
            return await _dbSet
                .Include(d => d.Procesos)
                .ToListAsync();
        }

        // Override para incluir Procesos por defecto en ObtenerPorId
        public override async Task<Dominio?> ObtenerPorId(int id)
        {
            return await _dbSet
                .Include(d => d.Procesos)
                .FirstOrDefaultAsync(d => d.IdDominio == id);
        }

        // Override para incluir Procesos por defecto en ObtenerTodos
        public override async Task<IEnumerable<Dominio>> ObtenerTodos()
        {
            return await _dbSet
                .Include(d => d.Procesos)
                .ToListAsync();
        }
    }
}
