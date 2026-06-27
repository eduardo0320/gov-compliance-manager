using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class ProcesoRepository : Repository<Proceso, int>, IProcesoRepository
    {
        public ProcesoRepository(NormasDb context) : base(context)
        {
        }

        public async Task<Proceso?> ObtenerPorCodigo(string codigo)
        {
            return await _dbSet
                .Include(p => p.Dominio)
                .FirstOrDefaultAsync(p => p.Codigo == codigo);
        }

        public async Task<bool> ExistePorCodigo(string codigo)
        {
            return await _dbSet.AnyAsync(p => p.Codigo == codigo);
        }

        public async Task<IEnumerable<Proceso>> EncontrarPorIdDominio(int dominioId)
        {
            return await _dbSet
                .Include(p => p.Dominio)
                .Include(p => p.Subdominios)
                .Where(p => p.DominioId == dominioId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Proceso>> EncontrarPorEstadoImplementacion(string estadoImplementacion)
        {
            return await _dbSet
                .Include(p => p.Dominio)
                .Where(p => p.EstadoImplementacion == estadoImplementacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subdominio>> ObtenerSubdominiosPorIdProceso(int procesoId)
        {
            return await _context.Subdominios
                .Where(s => s.ProcesoId == procesoId)
                .Include(s => s.Proceso)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Proceso> Items, int TotalCount)> FindByNombreContainingAsync(
            string nombre, int page, int pageSize)
        {
            var query = _dbSet
                .Include(p => p.Dominio)
                .Where(p => p.Nombre.Contains(nombre));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Proceso> Items, int TotalCount)> FindByMarcoNormativoContainingAsync(
            string marcoNormativo, int page, int pageSize)
        {
            var query = _dbSet
                .Include(p => p.Dominio)
                .Where(p => p.MarcoNormativo.Contains(marcoNormativo));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Proceso?> GetByIdWithSubdominiosAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Subdominios)
                .Include(p => p.Dominio)
                .FirstOrDefaultAsync(p => p.IdProceso == id);
        }

        public async Task<Proceso?> GetByIdWithDominioAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Dominio)
                .FirstOrDefaultAsync(p => p.IdProceso == id);
        }

        public async Task<IEnumerable<Proceso>> GetAllWithDominioAsync()
        {
            return await _dbSet
                .Include(p => p.Dominio)
                .ToListAsync();
        }

        // Override para incluir Dominio por defecto en ObtenerPorId
        public override async Task<Proceso?> ObtenerPorId(int id)
        {
            return await _dbSet
                .Include(p => p.Dominio)
                .Include(p => p.Subdominios)
                .FirstOrDefaultAsync(p => p.IdProceso == id);
        }
    }
}