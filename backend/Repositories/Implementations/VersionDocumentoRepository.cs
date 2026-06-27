using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class VersionDocumentoRepository : Repository<VersionDocumento, int>, IVersionDocumentoRepository
    {
        public VersionDocumentoRepository(NormasDb context) : base(context)
        {
        }

        public async Task<IEnumerable<VersionDocumento>> ObtenerPorDocumentoId(int documentoId)
        {
            return await _dbSet
                .Include(v => v.SubidoPor)
                .Where(v => v.DocumentoId == documentoId)
                .OrderByDescending(v => v.NumeroVersion)
                .ToListAsync();
        }

        public async Task<VersionDocumento?> ObtenerPorDocumentoIdYNumeroVersion(int documentoId, int numeroVersion)
        {
            return await _dbSet
                .Include(v => v.SubidoPor)
                .FirstOrDefaultAsync(v => v.DocumentoId == documentoId
                    && v.NumeroVersion == numeroVersion);
        }

        public async Task<int> ObtenerSiguienteNumeroVersion(int documentoId)
        {
            var maxVersion = await _dbSet
                .Where(v => v.DocumentoId == documentoId)
                .MaxAsync(v => (int?)v.NumeroVersion);

            return (maxVersion ?? 0) + 1;
        }

        public async Task<VersionDocumento?> BuscarPorChecksum(int documentoId, string checksumSHA256)
        {
            return await _dbSet
                .FirstOrDefaultAsync(v => v.DocumentoId == documentoId
                    && v.ChecksumSHA256 == checksumSHA256);
        }

        public async Task<int> ContarVersionesActivasAsync()
        {
            return await _dbSet.CountAsync(v => v.Activo);
        }
    }
}