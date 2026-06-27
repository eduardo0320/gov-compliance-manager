using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IRelacionDocumentoRepository : IRepository<RelacionDocumento, int>
    {
        // Obtener relaciones donde el documento es el origen (documentos que éste referencia)
        Task<IEnumerable<RelacionDocumento>> ObtenerPorDocumentoOrigenId(int documentoId);

        // Obtener relaciones donde el documento es el destino (quién lo referencia)
        Task<IEnumerable<RelacionDocumento>> ObtenerPorDocumentoDestinoId(int documentoId);

        // Obtener relaciones activas de un documento (como origen o destino)
        Task<IEnumerable<RelacionDocumento>> ObtenerTodasPorDocumentoId(int documentoId);

        // Verificar si ya existe una relación con el mismo origen, destino y tipo
        Task<bool> ExisteRelacion(int origenId, int destinoId, string tipoRelacion);

        // Desactivar una relación (soft delete)
        Task<bool> DesactivarRelacion(int id);
    }
}
