using backend.DTOs;

namespace backend.Services.Interfaces
{
    public interface IVersionDocumentoService
    {
        /// <summary>Lista todas las versiones activas de un documento, ordenadas de más reciente a más antigua.</summary>
        Task<IEnumerable<object>> ObtenerVersionesAsync(int documentoId);

        /// <summary>
        /// Sube una nueva versión a un documento existente.
        /// Incluye el flujo transaccional: guardar temporal → calcular checksum
        /// → verificar duplicado → mover a repositorio → crear versión → actualizar VersionActualId.
        /// </summary>
        Task<object> SubirNuevaVersionAsync(int documentoId, SubirVersionDto dto, int usuarioId);

        /// <summary>
        /// Resuelve el archivo físico para descarga.
        /// Si version es null, descarga la versión actual.
        /// Retorna null si el documento o la versión no existe o es tipo URL.
        /// </summary>
        Task<(Stream Contenido, string NombreArchivo, string ContentType)?> DescargarVersionAsync(
            int documentoId, int? numeroVersion = null);
    }
}
