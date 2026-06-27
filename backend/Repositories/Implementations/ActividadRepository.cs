using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class ActividadRepository : Repository<Actividad, int>, IActividadRepository
    {
        public ActividadRepository(NormasDb context) : base(context)
        {
        }

        public async Task<IEnumerable<Actividad>> ObtenerPorIdSubdominio(int subdominioId)
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.SubdominioId == subdominioId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Actividad>> ObtenerPorEstadoImplementacion(string estadoImplementacion)
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.EstadoImplementacion == estadoImplementacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<Actividad>> ObtenerPorImplementable(string implementable)
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.Implementable == implementable)
                .ToListAsync();
        }

        public async Task<IEnumerable<Actividad>> ObtenerPorIdFuncionariosResponsables(int funcionarioId)
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.FuncionariosResponsablesId == funcionarioId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Actividad>> ObtenerPorRangoDeFechaCompromiso(DateTime fechaDesde, DateTime fechaHasta)
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.FechaCompromiso >= fechaDesde && a.FechaCompromiso <= fechaHasta)
                .ToListAsync();
        }

        public async Task<IEnumerable<Actividad>> ObtenerPorRangoDePorcentajeAvance(decimal porcentajeMin, decimal porcentajeMax)
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.PorcentajeAvance >= porcentajeMin && a.PorcentajeAvance <= porcentajeMax)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Actividad> Items, int TotalCount)> FindByNombreContainingAsync(
            string nombre, int page, int pageSize)
        {
            var query = _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.Nombre.Contains(nombre));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Actividad> Items, int TotalCount)> EncontrarPorEstadoImplementacion(
            string estadoImplementacion, int page, int pageSize)
        {
            var query = _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.EstadoImplementacion == estadoImplementacion);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Actividad> Items, int TotalCount)> FindByObservacionesContainingAsync(
            string observaciones, int page, int pageSize)
        {
            var query = _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.Observaciones != null && a.Observaciones.Contains(observaciones));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Actividad?> GetByIdWithFuncionarioResponsableAsync(int id)
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .FirstOrDefaultAsync(a => a.IdActividad == id);
        }

        public async Task<Actividad?> GetByIdWithSubdominioAsync(int id)
        {
            return await _dbSet
                .Include(a => a.Subdominio)
                    .ThenInclude(s => s.Proceso)
                        .ThenInclude(p => p.Dominio)
                .Include(a => a.FuncionariosResponsables)
                .FirstOrDefaultAsync(a => a.IdActividad == id);
        }

        public async Task<IEnumerable<Actividad>> GetAllWithFuncionarioResponsableAsync()
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Actividad>> ObtenerActividadesPendientesAsync()
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.EstadoImplementacion != "Implementado")
                .ToListAsync();
        }

        // Solo obtiene las actividades que se vencen exactamente en los dias que se manden por parametro
        public async Task<IEnumerable<Actividad>> ObtenerActividadesPendientesPorDiasVencimiento(int dias)
        {
            var fechaLimite = DateTime.Today.AddDays(dias);
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.EstadoImplementacion != "Implementado" && a.FechaCompromiso == fechaLimite)
                .ToListAsync();
        }

        public async Task<IEnumerable<Actividad>> ObtenerActividadesVencidasAsync()
        {
            var fechaActual = DateTime.Today;
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .Where(a => a.FechaCompromiso < fechaActual && a.EstadoImplementacion != "Implementado")
                .ToListAsync();
        }

        public async Task<decimal> ObtenerPromedioAvancePorSubdominioAsync(int subdominioId)
        {
            var actividades = await _dbSet
                .Where(a => a.SubdominioId == subdominioId)
                .ToListAsync();

            if (!actividades.Any())
                return 0;

            return actividades.Average(a => a.PorcentajeAvance);
        }

        // Override para incluir relaciones por defecto en ObtenerPorIdAsync
        public override async Task<Actividad?> ObtenerPorId(int id)
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                    .ThenInclude(s => s.Proceso)
                .FirstOrDefaultAsync(a => a.IdActividad == id);
        }

        // Override para incluir relaciones por defecto en ObtenerTodos
        public override async Task<IEnumerable<Actividad>> ObtenerTodos()
        {
            return await _dbSet
                .Include(a => a.FuncionariosResponsables)
                .Include(a => a.Subdominio)
                .ToListAsync();
        }

        // Carga la jerarquía completa para construir rutas de almacenamiento de documentos:
        // Actividad → Subdominio → Proceso → Dominio
        public async Task<Actividad?> ObtenerPorIdConJerarquia(int id)
        {
            return await _dbSet
                .Include(a => a.Subdominio)
                    .ThenInclude(s => s.Proceso)
                        .ThenInclude(p => p.Dominio)
                .FirstOrDefaultAsync(a => a.IdActividad == id);
        }
    }
}
