using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class SubdominioRepository : Repository<Subdominio, int>, ISubdominioRepository
    {
        public SubdominioRepository(NormasDb context) : base(context)
        {
        }

        public async Task<IEnumerable<Subdominio>> ObtenerPorProcesoId(int procesoId)
        {
            return await _dbSet
                .Include(s => s.Proceso)
                .Where(s => s.ProcesoId == procesoId)
                .ToListAsync();
        }


        public async Task<(IEnumerable<Subdominio> Items, int TotalCount)> FindByPracticasGobiernoContainingAsync(
            string practicasGobierno, int page, int pageSize)
        {
            var query = _dbSet
                .Include(s => s.Proceso)
                .Where(s => s.PracticasGobierno.Contains(practicasGobierno));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Subdominio> Items, int TotalCount)> FindByIndicadoresAsociadosContainingAsync(
            string indicadoresAsociados, int page, int pageSize)
        {
            var query = _dbSet
                .Include(s => s.Proceso)
                .Where(s => s.IndicadoresAsociados.Contains(indicadoresAsociados));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Subdominio?> GetByIdWithActividadesAsync(int id)
        {
            return await _dbSet
                .Include(s => s.Actividades)
                    .ThenInclude(a => a.FuncionariosResponsables)
                .Include(s => s.Proceso)
                .FirstOrDefaultAsync(s => s.IdSubdominio == id);
        }

        public async Task<Subdominio?> GetByIdWithProcesoAsync(int id)
        {
            return await _dbSet
                .Include(s => s.Proceso)
                    .ThenInclude(p => p.Dominio)
                .FirstOrDefaultAsync(s => s.IdSubdominio == id);
        }

        public async Task<IEnumerable<Subdominio>> GetAllWithProcesoAsync()
        {
            return await _dbSet
                .Include(s => s.Proceso)
                    .ThenInclude(p => p.Dominio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subdominio>> GetByProcesoIdWithActividadesAsync(int procesoId)
        {
            return await _dbSet
                .Include(s => s.Actividades)
                    .ThenInclude(a => a.FuncionariosResponsables)
                .Include(s => s.Proceso)
                .Where(s => s.ProcesoId == procesoId)
                .ToListAsync();
        }

        // Override para incluir Proceso por defecto en ObtenerPorId
        public override async Task<Subdominio?> ObtenerPorId(int id)
        {
            return await _dbSet
                .Include(s => s.Proceso)
                .Include(s => s.Actividades)
                .FirstOrDefaultAsync(s => s.IdSubdominio == id);
        }

        // Override para incluir Proceso por defecto en ObtenerTodos
        public override async Task<IEnumerable<Subdominio>> ObtenerTodos()
        {
            return await _dbSet
                .Include(s => s.Proceso)
                .ToListAsync();
        }
    }
}
