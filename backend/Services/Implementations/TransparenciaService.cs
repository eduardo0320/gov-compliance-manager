using backend.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Services.Implementations
{
    /// <summary>
    /// Implementación de <see cref="ITransparenciaService"/>.
    /// Delega la lógica de datos en <see cref="IDocumentoService"/> para no duplicar
    /// el acceso a la base de datos y respetar la capa de repositorios existente.
    /// </summary>
    public class TransparenciaService : ITransparenciaService
    {
        private readonly IDocumentoService _documentoService;
        private readonly ILogger<TransparenciaService> _logger;

        public TransparenciaService(
            IDocumentoService documentoService,
            ILogger<TransparenciaService> logger)
        {
            _documentoService = documentoService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<object> ObtenerDocumentosPublicosAsync()
        {
            _logger.LogInformation("Obteniendo documentos públicos para página de transparencia.");
            return await _documentoService.ObtenerDocumentosPublicosAsync();
        }

        /// <inheritdoc/>
        public async Task<(byte[]? Bytes, string? ContentType, string? FileName, string? UrlExterna)>
            ResolverDescargaPublicaAsync(int id)
        {
            _logger.LogInformation("Resolviendo descarga pública para documento {Id}.", id);
            return await _documentoService.ResolverDescargaPublicaAsync(id);
        }
    }
}