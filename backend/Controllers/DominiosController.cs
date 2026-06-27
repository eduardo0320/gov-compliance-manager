using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Dtos;
using backend.Services.Interfaces;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/dominios")]
    public class DominiosController : ControllerBase
    {
        private readonly IDominioService _dominioService;
        private readonly IProcesoService _procesoService;

        public DominiosController(IDominioService dominioService, IProcesoService procesoService)
        {
            _dominioService = dominioService;
            _procesoService = procesoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerTodos(CancellationToken ct)
        {
            try
            {
                var dominios = await _dominioService.ObtenerTodosLosDominiosAsync();
                return Ok(dominios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET /api/dominios/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> ObtenerPorId([FromRoute] int id, CancellationToken ct)
        {
            try
            {
                var dominio = await _dominioService.ObtenerDominioPorIdAsync(id);

                if (dominio == null)
                    return NotFound("Dominio no encontrado");

                return Ok(dominio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET /api/dominios/{dominioId}/procesos
        [HttpGet("{dominioId:int}/procesos")]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerProcesosPorDominio(
            [FromRoute] int dominioId, CancellationToken ct)
        {
            try
            {
                var procesos = await _procesoService.ObtenerProcesosPorDominioAsync(dominioId);
                return Ok(procesos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("tree")]
        public async Task<ActionResult<List<DominioTreeDto>>> ObtenerArbol(CancellationToken ct)
        {
            try
            {
                var dominiosConProcesos = await _dominioService.ObtenerDominiosConProcesosAsync();

                // Aquí necesitarías adaptar el resultado del service al DTO esperado
                // Por ahora retornaré el resultado directo del service
                return Ok(dominiosConProcesos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("arbol")]
        public async Task<ActionResult<List<DominioTreeWithSubDto>>> ObtenerArbolConSubdominios(CancellationToken ct)
        {
            try
            {
                var dominiosConSubdominios = await _dominioService.ObtenerDominiosConSubdominiosAsync();

                // Aquí necesitarías adaptar el resultado del service al DTO esperado
                // Por ahora retornaré el resultado directo del service
                return Ok(dominiosConSubdominios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        private static string AdivinarIcono(string nombre)
        {
            var upper = (nombre ?? "").ToUpperInvariant();
            if (upper.Contains("EDM")) return "fas fa-cog";
            if (upper.Contains("APO")) return "fas fa-bullseye";
            if (upper.Contains("BAI")) return "fas fa-tools";
            if (upper.Contains("DSS")) return "fas fa-headset";
            if (upper.Contains("MEA")) return "fas fa-chart-line";
            return "fas fa-folder";
        }
    }
}
