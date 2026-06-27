using backend.Models;
using backend.Data;
using Backend.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/procesos")]
    public class ProcesosController : ControllerBase
    {
        private readonly IProcesoService _procesoService;
        private readonly IDominioService _dominioService;
        private readonly ISubdominioService _subdominioService;
        private readonly IAuditoriaService? _auditoriaService;

        public ProcesosController(
            IProcesoService procesoService,
            IDominioService dominioService,
            ISubdominioService subdominioService,
            IAuditoriaService? auditoriaService = null)
        {
            _procesoService = procesoService;
            _dominioService = dominioService;
            _subdominioService = subdominioService;
            _auditoriaService = auditoriaService;
        }

        // SOLO ADMIN o SUPERADMIN
        [HttpPost]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<ActionResult<object>> CrearProceso([FromBody] CrearProcesoRequest req, CancellationToken ct)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Codigo) || string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest("Código y Nombre son requeridos");

            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var usuarioId = int.TryParse(claim, out var parsed) ? parsed : 1;

            try
            {
                var resultado = await _procesoService.CrearProcesoAsync(
                    req.Codigo.Trim(),
                    req.Nombre.Trim(),
                    req.MarcoNormativo?.Trim() ?? "",
                    req.DominioId,
                    usuarioId,
                    req.PrioridadImplementacion);

                if (resultado.StartsWith("Error:"))
                {
                    if (resultado.Contains("no encontrado"))
                        return NotFound(resultado);
                    else if (resultado.Contains("Ya existe"))
                        return Conflict(resultado);
                    else
                        return BadRequest(resultado);
                }

                // El resultado debería tener el formato "Proceso creado exitosamente: {ID}"
                if (!resultado.Contains(":"))
                {
                    return StatusCode(500, "Error: Formato de respuesta inesperado del servicio");
                }

                var parts = resultado.Split(':');
                if (parts.Length < 2 || !int.TryParse(parts[1].Trim(), out var procesoId))
                {
                    return StatusCode(500, "Error: No se pudo obtener el ID del proceso creado");
                }
                // Si el payload incluye subdominios, créelos ahora asociados al proceso recién creado
                if (req.Subdominios != null && req.Subdominios.Any())
                {
                    foreach (var s in req.Subdominios)
                    {
                        try
                        {
                            // Ignoramos el resultado específico, pero llamamos al servicio para persistir cada subdominio
                            await _subdominioService.CrearSubdominioAsync(
                                s.PracticasGobierno?.Trim() ?? string.Empty,
                                s.IndicadoresAsociados?.Trim() ?? string.Empty,
                                procesoId);
                        }
                        catch
                        {
                            // No abortamos la creación del proceso si falla algún subdominio; se podría mejorar para reportar errores
                        }
                    }
                }

                var procesoCreado = await _procesoService.ObtenerProcesoPorIdAsync(procesoId);

                if (_auditoriaService != null)
                {
                    await _auditoriaService.RegistrarEventoAsync(
                        "Creación",
                        $"Proceso creado: '{req.Nombre.Trim()}' (ID {procesoId}, código {req.Codigo.Trim()})",
                        "Procesos",
                        usuarioId,
                        datosNuevos: procesoCreado);
                }

                return CreatedAtAction(nameof(ObtenerProceso), new { id = procesoId }, procesoCreado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerProcesos([FromQuery] int? dominioId, CancellationToken ct)
        {
            try
            {
                var resultados = dominioId.HasValue
                    ? await _procesoService.ObtenerProcesosPorDominioAsync(dominioId.Value)
                    : await _procesoService.ObtenerTodosLosProcesosAsync();

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarProcesos([FromQuery] string q, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Parámetro de búsqueda requerido");

            try
            {
                var resultados = await _procesoService.BuscarProcesosPorNombreAsync(q.Trim());
                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<ActionResult<object>> EditarProceso([FromRoute] int id, [FromBody] EditarProcesoRequest req, CancellationToken ct)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Codigo) || string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest("Código y Nombre son requeridos");

            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var usuarioId = int.TryParse(claim, out var parsed) ? parsed : 1;

            try
            {
                var procesoAntes = await _procesoService.ObtenerProcesoPorIdAsync(id);

                var resultado = await _procesoService.ActualizarProcesoAsync(
                    id,
                    req.Codigo.Trim(),
                    req.Nombre.Trim(),
                    req.MarcoNormativo?.Trim() ?? "",
                    req.DominioId,
                    usuarioId,
                    req.PrioridadImplementacion);

                if (resultado.StartsWith("Error:"))
                {
                    if (resultado.Contains("no encontrado"))
                        return NotFound(resultado);
                    else if (resultado.Contains("Ya existe"))
                        return Conflict(resultado);
                    else
                        return BadRequest(resultado);
                }

                var procesoActualizado = await _procesoService.ObtenerProcesoPorIdAsync(id);

                var nombreAnterior = procesoAntes?.GetType().GetProperty("nombre")?.GetValue(procesoAntes)?.ToString();
                var nombreNuevo = procesoActualizado?.GetType().GetProperty("nombre")?.GetValue(procesoActualizado)?.ToString();
                var cambioNombre =
                    !string.IsNullOrWhiteSpace(nombreAnterior) &&
                    !string.IsNullOrWhiteSpace(nombreNuevo) &&
                    !string.Equals(nombreAnterior, nombreNuevo, StringComparison.OrdinalIgnoreCase);

                if (_auditoriaService != null)
                {
                    await _auditoriaService.RegistrarEventoAsync(
                        cambioNombre ? "CambioNombre" : "Modificación",
                        cambioNombre
                            ? $"Proceso {id} renombrado: '{nombreAnterior}' -> '{nombreNuevo}'"
                            : $"Proceso {id} actualizado",
                        "Procesos",
                        usuarioId,
                        datosAnteriores: procesoAntes,
                        datosNuevos: procesoActualizado);
                }

                return Ok(procesoActualizado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> ObtenerProceso([FromRoute] int id, CancellationToken ct)
        {
            try
            {
                var proceso = await _procesoService.ObtenerProcesoPorIdAsync(id);

                if (proceso == null)
                    return NotFound("Proceso no encontrado");

                return Ok(proceso);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var normalized = text.ToLowerInvariant();
            normalized = normalized
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace("à", "a").Replace("è", "e").Replace("ì", "i").Replace("ò", "o").Replace("ù", "u")
                .Replace("ä", "a").Replace("ë", "e").Replace("ï", "i").Replace("ö", "o").Replace("ü", "u")
                .Replace("â", "a").Replace("ê", "e").Replace("î", "i").Replace("ô", "o").Replace("û", "u")
                .Replace("ç", "c").Replace("ñ", "n");
            return normalized;
        }
    }
}
