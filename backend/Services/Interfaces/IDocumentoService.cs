using backend.DTOs;

namespace backend.Services.Interfaces
{
    public interface IDocumentoService
    {
        /// <summary>Lista todos los documentos no eliminados de una actividad.</summary>
        Task<IEnumerable<object>> ObtenerDocumentosActividadAsync(int actividadId);

        /// <summary>Obtiene el detalle completo de un documento (con versiones y relaciones).</summary>
        Task<object?> ObtenerDocumentoAsync(int id);

        /// <summary>
        /// Crea un documento junto con su primera versión (v1.0).
        /// Flujo transaccional de 4 pasos: archivo temporal → crear documento
        /// → mover a repositorio + crear versión → actualizar VersionActualId.
        /// </summary>
        Task<object> CrearDocumentoAsync(DocumentoCreateDto dto, int usuarioId);

        /// <summary>Realiza un soft delete del documento.</summary>
        Task<string> EliminarDocumentoAsync(int id, int usuarioId);

        /// <summary>Cambia el estado de un documento (validando la transición).</summary>
        Task<string> CambiarEstadoAsync(int id, CambiarEstadoDocumentoDto dto, int usuarioId);

        /// <summary>Crea una relación entre dos documentos.</summary>
        Task<object> CrearRelacionAsync(int documentoOrigenId, CrearRelacionDto dto, int usuarioId);

        /// <summary>Lista todas las relaciones activas de un documento (como origen o destino).</summary>
        Task<IEnumerable<object>> ObtenerRelacionesAsync(int documentoId);

        /// <summary>Desactiva una relación entre documentos (soft delete).</summary>
        Task<string> EliminarRelacionAsync(int relacionId, int usuarioId);

        // ── Fase 5 ────────────────────────────────────────────────────────────

        /// <summary>
        /// Busca documentos con filtros opcionales (nombre, estado, tipo, actividadId,
        /// vencimiento desde/hasta). Devuelve solo documentos no eliminados.
        /// </summary>
        Task<IEnumerable<object>> BuscarDocumentosAsync(BuscarDocumentosDto filtros);

        /// <summary>
        /// Devuelve documentos vencidos y próximos a vencer en <paramref name="dias"/> días,
        /// junto con la cantidad de documentos en cada categoría para mostrar badges.
        /// </summary>
        Task<object> ObtenerAlertasVencimientoAsync(int dias = 30);

        /// <summary>Retorna estadísticas agregadas de documentos del sistema.</summary>
        Task<object> ObtenerEstadisticasDocumentosAsync();

        /// <summary>Actualiza los metadatos editables de un documento (nombre, descripción, categoría, fechas, confidencialidad).</summary>
        Task<object> ActualizarDocumentoAsync(int id, ActualizarDocumentoDto dto, int usuarioId);

        // ── Transparencia pública ──────────────────────────────────────────────

        /// <summary>
        /// Devuelve todos los documentos Vigentes y Públicos agrupados por la jerarquía
        /// Dominio → Proceso → Subdominio → Actividad. No requiere autenticación.
        /// </summary>
        Task<object> ObtenerDocumentosPublicosAsync();

        /// <summary>
        /// Resuelve la descarga de un documento público vigente.
        /// Devuelve (Bytes, ContentType, FileName) para archivos físicos,
        /// o (null, null, null, UrlExterna) para documentos de tipo URL.
        /// Lanza KeyNotFoundException si el documento no existe o no es público.
        /// </summary>
        Task<(byte[]? Bytes, string? ContentType, string? FileName, string? UrlExterna)>
            ResolverDescargaPublicaAsync(int id);
    }
}
