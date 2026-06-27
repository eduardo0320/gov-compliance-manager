using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class AuditoriaRepository : Repository<Auditoria, int>, IAuditoriaRepository
    {
        public AuditoriaRepository(NormasDb context) : base(context)
        {
        }

        public async Task<IEnumerable<Auditoria>> FindByIdUsuarioAsync(int? idUsuario)
        {
            return await _dbSet
                .Include(a => a.Usuario)
                .Where(a => a.IdUsuario == idUsuario)
                .OrderByDescending(a => a.FechaEvento)
                .ToListAsync();
        }

        public async Task<IEnumerable<Auditoria>> FindByTipoEventoAsync(string tipoEvento)
        {
            return await _dbSet
                .Include(a => a.Usuario)
                .Where(a => a.TipoEvento == tipoEvento)
                .OrderByDescending(a => a.FechaEvento)
                .ToListAsync();
        }

        public async Task<IEnumerable<Auditoria>> FindByFechaEventoRangeAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            return await _dbSet
                .Include(a => a.Usuario)
                .Where(a => a.FechaEvento >= fechaDesde && a.FechaEvento <= fechaHasta)
                .OrderByDescending(a => a.FechaEvento)
                .ToListAsync();
        }

        public async Task<IEnumerable<Auditoria>> FindByTablaAfectadaAsync(string tablaAfectada)
        {
            return await _dbSet
                .Include(a => a.Usuario)
                .Where(a => a.Modulo == tablaAfectada)
                .OrderByDescending(a => a.FechaEvento)
                .ToListAsync();
        }

        public async Task<IEnumerable<Auditoria>> FindByRegistroAfectadoAsync(int registroAfectado)
        {
            // Como no hay RegistroAfectado en el modelo, usaremos IdUsuario como alternativa
            return await _dbSet
                .Include(a => a.Usuario)
                .Where(a => a.IdUsuario == registroAfectado)
                .OrderByDescending(a => a.FechaEvento)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Auditoria> Items, int TotalCount)> FindByDescripcionContainingAsync(
            string descripcion, int page, int pageSize)
        {
            var query = _dbSet
                .Include(a => a.Usuario)
                .Where(a => a.Descripcion.Contains(descripcion))
                .OrderByDescending(a => a.FechaEvento);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Auditoria> Items, int TotalCount)> FindByTipoEventoAsync(
            string tipoEvento, int page, int pageSize)
        {
            var query = _dbSet
                .Include(a => a.Usuario)
                .Where(a => a.TipoEvento == tipoEvento)
                .OrderByDescending(a => a.FechaEvento);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Auditoria> Items, int TotalCount)> FindByFechaEventoRangeAsync(
            DateTime fechaDesde, DateTime fechaHasta, int page, int pageSize)
        {
            var query = _dbSet
                .Include(a => a.Usuario)
                .Where(a => a.FechaEvento >= fechaDesde && a.FechaEvento <= fechaHasta)
                .OrderByDescending(a => a.FechaEvento);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Auditoria>> GetAllWithUsuarioAsync()
        {
            return await _dbSet
                .Include(a => a.Usuario)
                .OrderByDescending(a => a.FechaEvento)
                .ToListAsync();
        }

        public async Task<Auditoria?> GetByIdWithUsuarioAsync(int id)
        {
            return await _dbSet
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.IdAuditoria == id);
        }

        public async Task<IEnumerable<Auditoria>> GetUltimosEventosAsync(int cantidad)
        {
            return await _dbSet
                .Include(a => a.Usuario)
                .OrderByDescending(a => a.FechaEvento)
                .Take(cantidad)
                .ToListAsync();
        }

        public async Task<IEnumerable<Auditoria>> GetEventosPorUsuarioAsync(int idUsuario, DateTime? fechaDesde = null)
        {
            var query = _dbSet
                .Include(a => a.Usuario)
                .Where(a => a.IdUsuario == idUsuario);

            if (fechaDesde.HasValue)
            {
                query = query.Where(a => a.FechaEvento >= fechaDesde.Value);
            }

            return await query
                .OrderByDescending(a => a.FechaEvento)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetEstadisticasPorTipoEventoAsync(DateTime fechaDesde, DateTime fechaHasta)
        {
            return await _dbSet
                .Where(a => a.FechaEvento >= fechaDesde && a.FechaEvento <= fechaHasta)
                .GroupBy(a => a.TipoEvento)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        // Override para incluir Usuario por defecto en ObtenerPorId
        public override async Task<Auditoria?> ObtenerPorId(int id)
        {
            return await _dbSet
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.IdAuditoria == id);
        }

        // Override para incluir Usuario por defecto en ObtenerTodos
        public override async Task<IEnumerable<Auditoria>> ObtenerTodos()
        {
            return await _dbSet
                .Include(a => a.Usuario)
                .OrderByDescending(a => a.FechaEvento)
                .ToListAsync();
        }
    }
}
