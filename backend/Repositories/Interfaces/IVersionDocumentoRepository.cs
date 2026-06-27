using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IVersionDocumentoRepository : IRepository<VersionDocumento, int>
    {
        // Obtener todas las versiones activas de un documento, ordenadas por número
        Task<IEnumerable<VersionDocumento>> ObtenerPorDocumentoId(int documentoId);

        // Obtener una versión específica de un documento por número de versión
        Task<VersionDocumento?> ObtenerPorDocumentoIdYNumeroVersion(int documentoId, int numeroVersion);

        // Obtener el siguiente número de versión disponible para un documento
        Task<int> ObtenerSiguienteNumeroVersion(int documentoId);

        // Verificar si ya existe una versión con el mismo checksum para un documento
        Task<VersionDocumento?> BuscarPorChecksum(int documentoId, string checksumSHA256);

        // Contar el total de versiones activas (para estadísticas)
        Task<int> ContarVersionesActivasAsync();
    }
}