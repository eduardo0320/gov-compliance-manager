using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using backend.Services.Interfaces;

namespace backend.Controllers
{
    /// <summary>
    /// Endpoint público (sin autenticación) para la página de transparencia MICITT.
    /// Expone únicamente documentos con Confidencialidad='Publica' y Estado='Vigente'.
    /// Toda la lógica de datos pasa por ITransparenciaService.
    /// Rate limiting: 60 requests/minuto por IP.
    /// </summary>
    [ApiController]
    [Route("api/transparencia")]
    [AllowAnonymous]
    [EnableRateLimiting("transparencia")]
    public class TransparenciaController : ControllerBase
    {
        private readonly ITransparenciaService _transparenciaService;
        private readonly ILogger<TransparenciaController> _logger;

        public TransparenciaController(
            ITransparenciaService transparenciaService,
            ILogger<TransparenciaController> logger)
        {
            _transparenciaService = transparenciaService;
            _logger = logger;
        }

        // ──────────────────────────────────────────────
        //  GET /api/transparencia/documentos
        //  Devuelve todos los documentos Vigentes y Públicos,
        //  agrupados por la jerarquía Dominio → Proceso → Subdominio → Actividad.
        // ──────────────────────────────────────────────
        [HttpGet("documentos")]
        public async Task<IActionResult> ObtenerDocumentosPublicos()
        {
            try
            {
                var resumen = await _transparenciaService.ObtenerDocumentosPublicosAsync();
                return Ok(resumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documentos públicos de transparencia");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  GET /api/transparencia/documentos/{id}/descargar
        //  Descarga o devuelve URL de un documento público vigente.
        //  - Archivos físicos → File(bytes, contentType, fileName)
        //  - Documentos URL  → 200 { url: "..." }
        // ──────────────────────────────────────────────
        [HttpGet("documentos/{id:int}/descargar")]
        public async Task<IActionResult> DescargarDocumento(int id)
        {
            try
            {
                var (bytes, contentType, fileName, urlExterna) =
                    await _transparenciaService.ResolverDescargaPublicaAsync(id);

                if (urlExterna != null)
                    return Ok(new { url = urlExterna });

                return File(bytes!, contentType!, fileName);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Documento público {Id} no encontrado: {Msg}", id, ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar documento público {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }
    }
}