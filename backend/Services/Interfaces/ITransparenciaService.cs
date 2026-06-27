namespace backend.Services.Interfaces
{
    /// <summary>
    /// Servicio dedicado a la página pública de transparencia MICITT.
    /// Expone únicamente documentos con Confidencialidad='Publica' y Estado='Vigente'.
    /// No requiere autenticación en ninguno de sus métodos.
    /// </summary>
    public interface ITransparenciaService
    {
        /// <summary>
        /// Devuelve todos los documentos Vigentes y Públicos agrupados por la jerarquía
        /// Dominio → Proceso → Subdominio → Actividad.
        /// </summary>
        Task<object> ObtenerDocumentosPublicosAsync();

        /// <summary>
        /// Resuelve la descarga de un documento público vigente.
        /// Devuelve (Bytes, ContentType, FileName) para archivos físicos,
        /// o (null, null, null, UrlExterna) para documentos de tipo URL.
        /// Lanza <see cref="KeyNotFoundException"/> si el documento no existe o no es público.
        /// </summary>
        Task<(byte[]? Bytes, string? ContentType, string? FileName, string? UrlExterna)>
            ResolverDescargaPublicaAsync(int id);
    }
}