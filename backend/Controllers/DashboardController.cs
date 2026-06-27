using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Árbol completo: Dominios → Procesos → Subdominios → Actividades.
        /// Una sola petición HTTP en lugar de las N×M×K del cliente anterior.
        /// </summary>
        [HttpGet("arbol-completo")]
        public async Task<IActionResult> ObtenerArbolCompleto(CancellationToken ct)
        {
            try
            {
                var arbol = await _dashboardService.ObtenerArbolCompletoAsync();
                return Ok(arbol);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
        [HttpGet("stats")]
        public async Task<IActionResult> ObtenerStats(CancellationToken ct)
        {
            try
            {
                var stats = await _dashboardService.ObtenerStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
