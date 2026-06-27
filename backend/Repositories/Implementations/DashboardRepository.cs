using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly NormasDb _context;

        public DashboardRepository(NormasDb context)
        {
            _context = context;
        }

        /// <summary>
        /// Una sola consulta SQL con Include/ThenInclude.
        /// Reemplaza el waterfall de N×M×K peticiones que hacía el Dashboard anterior.
        /// </summary>
        public async Task<IEnumerable<Dominio>> ObtenerArbolCompletoAsync()
        {
            return await _context.Dominios
                .AsNoTracking()
                .Include(d => d.Procesos)
                    .ThenInclude(p => p.Subdominios)
                        .ThenInclude(s => s.Actividades)
                .OrderBy(d => d.Nombre)
                .ToListAsync();
        }
    }
}
