using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.DTOs;
using backend.Services.Interfaces;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/documentos")]
    [Authorize]
    public class DocumentosController : ControllerBase
    {
        private readonly IDocumentoService _documentoService;
        private readonly IVersionDocumentoService _versionService;
        private readonly ILogger<DocumentosController> _logger;

        public DocumentosController(
            IDocumentoService documentoService,
            IVersionDocumentoService versionService,
            ILogger<DocumentosController> logger)
        {
            _documentoService = documentoService;
            _versionService = versionService;
            _logger = logger;
        }

        // ──────────────────────────────────────────────
        //  POST /api/documentos
        // ──────────────────────────────────────────────

        /// <summary>
        /// Crea un nuevo documento y su primera versión (archivo o URL).
        /// Usa multipart/form-data cuando se adjunta un archivo.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Crear([FromForm] DocumentoCreateDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            try
            {
                var resultado = await _documentoService.CrearDocumentoAsync(dto, usuarioId.Value);
                return StatusCode(201, resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear documento para actividad {ActividadId}", dto.ActividadId);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  GET /api/documentos/{id}
        // ──────────────────────────────────────────────

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Obtener(int id)
        {
            try
            {
                var documento = await _documentoService.ObtenerDocumentoAsync(id);
                if (documento == null)
                    return NotFound(new { error = $"Documento {id} no encontrado" });

                return Ok(documento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documento {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  DELETE /api/documentos/{id}
        // ──────────────────────────────────────────────

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            try
            {
                var mensaje = await _documentoService.EliminarDocumentoAsync(id, usuarioId.Value);
                return Ok(new { mensaje });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar documento {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }
        // ──────────────────────────────────────────────
        //  PUT /api/documentos/{id}
        // ──────────────────────────────────────────────

        /// <summary>
        /// Actualiza los metadatos editables de un documento (nombre, descripción, categoría,
        /// fechas de vencimiento y alerta, confidencialidad).
        /// No cambia tipo, actividad, estado ni rol del documento.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarDocumentoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            try
            {
                var resultado = await _documentoService.ActualizarDocumentoAsync(id, dto, usuarioId.Value);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar documento {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }
        // ──────────────────────────────────────────────
        //  PUT /api/documentos/{id}/estado
        // ──────────────────────────────────────────────

        [HttpPut("{id:int}/estado")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoDocumentoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Estado))
                return BadRequest(new { error = "El campo 'Estado' es requerido" });

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            try
            {
                var mensaje = await _documentoService.CambiarEstadoAsync(id, dto, usuarioId.Value);
                return Ok(new { mensaje });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del documento {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  GET /api/documentos/{id}/versiones
        // ──────────────────────────────────────────────

        [HttpGet("{id:int}/versiones")]
        public async Task<IActionResult> ObtenerVersiones(int id)
        {
            try
            {
                var versiones = await _versionService.ObtenerVersionesAsync(id);
                return Ok(versiones);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener versiones del documento {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  POST /api/documentos/{id}/versiones
        // ──────────────────────────────────────────────

        /// <summary>
        /// Sube una nueva versión a un documento existente.
        /// </summary>
        [HttpPost("{id:int}/versiones")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubirVersion(int id, [FromForm] SubirVersionDto dto)
        {
            _logger.LogInformation("SubirVersion - DTO FechaVencimiento recibida = {FechaVencimiento}", dto.FechaVencimiento?.ToString("yyyy-MM-dd") ?? "null");
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            try
            {
                var resultado = await _versionService.SubirNuevaVersionAsync(id, dto, usuarioId.Value);
                return StatusCode(201, resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir versión para documento {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  GET /api/documentos/{id}/descargar?version={n}
        // ──────────────────────────────────────────────

        /// <summary>
        /// Descarga la versión actual o una versión específica del documento.
        /// Si el documento es de tipo URL, devuelve la URL en el cuerpo de la respuesta.
        /// </summary>
        [HttpGet("{id:int}/descargar")]
        public async Task<IActionResult> Descargar(int id, [FromQuery] int? version = null)
        {
            try
            {
                var resultado = await _versionService.DescargarVersionAsync(id, version);

                if (resultado == null)
                    return NotFound(new { error = "Documento o versión no encontrada" });

                var (stream, fileName, contentType) = resultado.Value;

                // stream == null indica que es de tipo URL → devolver la URL directamente
                if (stream == null)
                    return Ok(new { url = fileName });

                return File(stream, contentType, fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar documento {Id} versión {Version}", id, version);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  POST /api/documentos/{id}/relaciones
        // ──────────────────────────────────────────────

        [HttpPost("{id:int}/relaciones")]
        public async Task<IActionResult> CrearRelacion(int id, [FromBody] CrearRelacionDto dto)
        {
            if (dto.DocumentoDestinoId <= 0)
                return BadRequest(new { error = "DocumentoDestinoId es requerido" });

            if (string.IsNullOrWhiteSpace(dto.TipoRelacion))
                return BadRequest(new { error = "TipoRelacion es requerida" });

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            try
            {
                var resultado = await _documentoService.CrearRelacionAsync(id, dto, usuarioId.Value);
                return StatusCode(201, resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear relación para documento {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  GET /api/documentos/{id}/relaciones
        // ──────────────────────────────────────────────

        [HttpGet("{id:int}/relaciones")]
        public async Task<IActionResult> ObtenerRelaciones(int id)
        {
            try
            {
                var relaciones = await _documentoService.ObtenerRelacionesAsync(id);
                return Ok(relaciones);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener relaciones del documento {Id}", id);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  DELETE /api/documentos/relaciones/{relacionId}
        // ──────────────────────────────────────────────

        [HttpDelete("relaciones/{relacionId:int}")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> EliminarRelacion(int relacionId)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            try
            {
                var mensaje = await _documentoService.EliminarRelacionAsync(relacionId, usuarioId.Value);
                return Ok(new { mensaje });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar relación {RelacionId}", relacionId);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  GET /api/documentos/buscar
        // ──────────────────────────────────────────────

        /// <summary>
        /// Busca documentos con filtros opcionales (query string).
        /// Ejemplo: GET /api/documentos/buscar?nombre=manual&amp;estado=Vigente&amp;limite=20
        /// </summary>
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] BuscarDocumentosDto filtros)
        {
            try
            {
                var resultados = await _documentoService.BuscarDocumentosAsync(filtros);
                return Ok(resultados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en búsqueda de documentos");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  GET /api/documentos/vencimientos?dias=30
        // ──────────────────────────────────────────────

        /// <summary>
        /// Devuelve documentos vencidos y próximos a vencer en los próximos <paramref name="dias"/> días.
        /// Ej: GET /api/documentos/vencimientos?dias=30
        /// </summary>
        [HttpGet("vencimientos")]
        public async Task<IActionResult> ObtenerAlertasVencimiento([FromQuery] int dias = 30)
        {
            try
            {
                var alertas = await _documentoService.ObtenerAlertasVencimientoAsync(dias);
                return Ok(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo alertas de vencimiento");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ──────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────

        private int? ObtenerUsuarioId()
        {
            var sid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(sid, out var id) ? id : null;
        }
    }
}
