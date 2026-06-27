using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class DocumentoRepository : Repository<Documento, int>, IDocumentoRepository
    {
        public DocumentoRepository(NormasDb context) : base(context)
        {
        }

        public async Task<IEnumerable<Documento>> ObtenerPorActividadId(int actividadId)
        {
            return await _dbSet
                .Include(d => d.VersionActual)
                    .ThenInclude(v => v!.SubidoPor)
                .Include(d => d.CreadoPor)
                .Where(d => d.ActividadId == actividadId && !d.Eliminado)
                .OrderByDescending(d => d.FechaCreacion)
                .ToListAsync();
        }

        public async Task<Documento?> ObtenerConVersionActual(int id)
        {
            return await _dbSet
                .Include(d => d.VersionActual)
                    .ThenInclude(v => v!.SubidoPor)
                .Include(d => d.Actividad)
                .Include(d => d.CreadoPor)
                .Include(d => d.ModificadoPor)
                .FirstOrDefaultAsync(d => d.IdDocumento == id && !d.Eliminado);
        }

        public async Task<Documento?> ObtenerConVersiones(int id)
        {
            return await _dbSet
                .Include(d => d.VersionActual)
                .Include(d => d.Versiones)
                    .ThenInclude(v => v.SubidoPor)
                .Include(d => d.Actividad)
                .Include(d => d.CreadoPor)
                .FirstOrDefaultAsync(d => d.IdDocumento == id && !d.Eliminado);
        }

        public async Task<Documento?> ObtenerConRelaciones(int id)
        {
            return await _dbSet
                .Include(d => d.VersionActual)
                .Include(d => d.RelacionesComoOrigen.Where(r => r.Activo))
                    .ThenInclude(r => r.DocumentoDestino)
                .Include(d => d.RelacionesComoDestino.Where(r => r.Activo))
                    .ThenInclude(r => r.DocumentoOrigen)
                .FirstOrDefaultAsync(d => d.IdDocumento == id && !d.Eliminado);
        }

        public async Task<IEnumerable<Documento>> ObtenerProximosAVencer(int dias)
        {
            var fechaLimite = DateTime.UtcNow.AddDays(dias);
            return await _dbSet
                .Include(d => d.VersionActual)
                .Include(d => d.CreadoPor)
                .Where(d => !d.Eliminado
                    && d.FechaVencimiento.HasValue
                    && d.FechaVencimiento.Value <= fechaLimite
                    && d.FechaVencimiento.Value >= DateTime.UtcNow
                    && d.Estado != "Obsoleto"
                    && d.Estado != "Archivado")
                .OrderBy(d => d.FechaVencimiento)
                .ToListAsync();
        }

        public async Task<IEnumerable<Documento>> ObtenerPorEstado(string estado)
        {
            return await _dbSet
                .Include(d => d.VersionActual)
                .Include(d => d.CreadoPor)
                .Where(d => d.Estado == estado && !d.Eliminado)
                .OrderByDescending(d => d.FechaCreacion)
                .ToListAsync();
        }

        public async Task<bool> EliminarLogico(int id, int usuarioId)
        {
            var documento = await _dbSet.FindAsync(id);
            if (documento == null || documento.Eliminado)
                return false;

            documento.Eliminado = true;
            documento.EliminadoPorId = usuarioId;
            documento.FechaEliminacion = DateTime.UtcNow;

            _dbSet.Update(documento);
            return true;
        }

        public async Task<bool> ExistePrincipalEnActividadAsync(int actividadId)
        {
            return await _dbSet
                .AnyAsync(d => d.ActividadId == actividadId
                            && d.RolEnActividad == "Principal"
                            && !d.Eliminado);
        }

        public async Task<IEnumerable<Documento>> BuscarConFiltrosAsync(BuscarDocumentosDto filtros)
        {
            var query = _dbSet
                .Include(d => d.VersionActual)
                    .ThenInclude(v => v!.SubidoPor)
                .Include(d => d.Actividad)
                    .ThenInclude(a => a.FuncionariosResponsables)
                .Include(d => d.Actividad)
                    .ThenInclude(a => a.Subdominio)
                        .ThenInclude(s => s.Proceso)
                .Include(d => d.CreadoPor)
                .Where(d => !d.Eliminado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtros.Nombre))
                query = query.Where(d =>
                    d.Nombre.Contains(filtros.Nombre) ||
                    (d.Descripcion != null && d.Descripcion.Contains(filtros.Nombre)));

            if (!string.IsNullOrWhiteSpace(filtros.Estado))
                query = query.Where(d => d.Estado == filtros.Estado);

            if (!string.IsNullOrWhiteSpace(filtros.TipoDocumento))
                query = query.Where(d => d.TipoDocumento == filtros.TipoDocumento);

            if (filtros.ActividadId.HasValue)
                query = query.Where(d => d.ActividadId == filtros.ActividadId.Value);

            if (filtros.VencimientoDesde.HasValue)
                query = query.Where(d => d.FechaVencimiento.HasValue
                    && d.FechaVencimiento.Value >= filtros.VencimientoDesde.Value);

            if (filtros.VencimientoHasta.HasValue)
                query = query.Where(d => d.FechaVencimiento.HasValue
                    && d.FechaVencimiento.Value <= filtros.VencimientoHasta.Value);

            if (!string.IsNullOrWhiteSpace(filtros.CodigoProceso))
                query = query.Where(d => d.Actividad.Subdominio.Proceso.Codigo.Contains(filtros.CodigoProceso));

            if (filtros.SoloVencidos == true)
                query = query.Where(d => d.FechaVencimiento.HasValue
                    && d.FechaVencimiento.Value < DateTime.UtcNow);

            query = filtros.SoloVencidos == true
                ? query.OrderBy(d => d.FechaVencimiento)
                : query.OrderByDescending(d => d.FechaCreacion);

            return await query
                .Take(filtros.Limite > 0 ? filtros.Limite : 50)
                .ToListAsync();
        }

        public async Task<IEnumerable<Documento>> ObtenerVencidosConJerarquiaAsync()
        {
            var hoy = DateTime.UtcNow.Date;
            return await _dbSet
                .Include(d => d.VersionActual)
                .Include(d => d.Actividad)
                    .ThenInclude(a => a.Subdominio)
                        .ThenInclude(s => s.Proceso)
                            .ThenInclude(p => p.Dominio)
                .Where(d => !d.Eliminado
                    && d.FechaVencimiento.HasValue
                    && d.FechaVencimiento.Value.Date < hoy
                    && d.Estado != "Obsoleto"
                    && d.Estado != "Archivado")
                .OrderBy(d => d.FechaVencimiento)
                .ToListAsync();
        }

        public async Task<IEnumerable<Documento>> ObtenerProximosAVencerConJerarquiaAsync(int dias)
        {
            var hoy = DateTime.UtcNow.Date;
            var fechaLimite = hoy.AddDays(dias);
            return await _dbSet
                .Include(d => d.VersionActual)
                .Include(d => d.Actividad)
                    .ThenInclude(a => a.Subdominio)
                        .ThenInclude(s => s.Proceso)
                            .ThenInclude(p => p.Dominio)
                .Where(d => !d.Eliminado
                    && d.FechaVencimiento.HasValue
                    && d.FechaVencimiento.Value.Date >= hoy
                    && d.FechaVencimiento.Value.Date <= fechaLimite
                    && d.Estado != "Obsoleto"
                    && d.Estado != "Archivado")
                .OrderBy(d => d.FechaVencimiento)
                .ToListAsync();
        }

        public async Task<IEnumerable<Documento>> ObtenerTodosNoEliminadosAsync()
        {
            return await _dbSet
                .Where(d => !d.Eliminado)
                .ToListAsync();
        }

        public async Task<Documento?> ObtenerPublicoPorIdAsync(int id)
        {
            return await _dbSet
                .Include(d => d.VersionActual)
                .FirstOrDefaultAsync(d =>
                    d.IdDocumento == id &&
                    !d.Eliminado &&
                    d.Confidencialidad == "Publica");
        }

        public async Task<IEnumerable<Documento>> ObtenerPublicosConVersionAsync()
        {
            return await _dbSet
                .Include(d => d.VersionActual)
                .Where(d => !d.Eliminado && d.Confidencialidad == "Publica")
                .OrderBy(d => d.Nombre)
                .ToListAsync();
        }
    }
}