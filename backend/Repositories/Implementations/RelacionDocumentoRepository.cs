using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class RelacionDocumentoRepository : Repository<RelacionDocumento, int>, IRelacionDocumentoRepository
    {
        public RelacionDocumentoRepository(NormasDb context) : base(context)
        {
        }

        public async Task<IEnumerable<RelacionDocumento>> ObtenerPorDocumentoOrigenId(int documentoId)
        {
            return await _dbSet
                .Include(r => r.DocumentoDestino)
                    .ThenInclude(d => d.VersionActual)
                .Include(r => r.CreadoPor)
                .Where(r => r.DocumentoOrigenId == documentoId && r.Activo)
                .OrderBy(r => r.TipoRelacion)
                .ThenBy(r => r.Orden)
                .ToListAsync();
        }

        public async Task<IEnumerable<RelacionDocumento>> ObtenerPorDocumentoDestinoId(int documentoId)
        {
            return await _dbSet
                .Include(r => r.DocumentoOrigen)
                    .ThenInclude(d => d.VersionActual)
                .Include(r => r.CreadoPor)
                .Where(r => r.DocumentoDestinoId == documentoId && r.Activo)
                .OrderBy(r => r.TipoRelacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<RelacionDocumento>> ObtenerTodasPorDocumentoId(int documentoId)
        {
            return await _dbSet
                .Include(r => r.DocumentoOrigen)
                .Include(r => r.DocumentoDestino)
                .Include(r => r.CreadoPor)
                .Where(r => (r.DocumentoOrigenId == documentoId || r.DocumentoDestinoId == documentoId) && r.Activo)
                .ToListAsync();
        }

        public async Task<bool> ExisteRelacion(int origenId, int destinoId, string tipoRelacion)
        {
            return await _dbSet
                .AnyAsync(r => r.DocumentoOrigenId == origenId
                    && r.DocumentoDestinoId == destinoId
                    && r.TipoRelacion == tipoRelacion
                    && r.Activo);
        }

        public async Task<bool> DesactivarRelacion(int id)
        {
            var relacion = await _dbSet.FindAsync(id);
            if (relacion == null || !relacion.Activo)
                return false;

            relacion.Activo = false;
            _dbSet.Update(relacion);
            return true;
        }
    }
}
