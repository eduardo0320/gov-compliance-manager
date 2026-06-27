using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services.Interfaces;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/estadisticas")]
    public class EstadisticasController : ControllerBase
    {
        private readonly IActividadService _actividadService;
        private readonly IDominioService _dominioService;
        private readonly IProcesoService _procesoService;
        private readonly IDocumentoService _documentoService;

        public EstadisticasController(
            IActividadService actividadService,
            IDominioService dominioService,
            IProcesoService procesoService,
            IDocumentoService documentoService)
        {
            _actividadService = actividadService;
            _dominioService = dominioService;
            _procesoService = procesoService;
            _documentoService = documentoService;
        }

        /// <summary>
        /// Obtiene estadísticas generales de todas las actividades
        /// </summary>
        [HttpGet("actividades")]
        [Authorize]
        public async Task<ActionResult<object>> ObtenerEstadisticasActividades(CancellationToken ct)
        {
            try
            {
                var estadisticas = await _actividadService.ObtenerEstadisticasActividadesAsync();
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene estadísticas generales del sistema
        /// </summary>
        [HttpGet("resumen")]
        [Authorize]
        public async Task<ActionResult<object>> ObtenerResumenEstadisticas(CancellationToken ct)
        {
            try
            {
                var dominios = await _dominioService.ObtenerTodosLosDominiosAsync();
                var procesos = await _procesoService.ObtenerTodosLosProcesosAsync();
                var estadisticasActividades = await _actividadService.ObtenerEstadisticasActividadesAsync();

                var resumen = new
                {
                    totales = new
                    {
                        dominios = ((IEnumerable<object>)dominios).Count(),
                        procesos = ((IEnumerable<object>)procesos).Count(),
                        actividades = GetPropertyValue(estadisticasActividades, "total_actividades")
                    },
                    actividades = estadisticasActividades,
                    fecha_consulta = DateTime.Now
                };

                return Ok(resumen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene estadísticas específicas de un subdominio
        /// </summary>
        [HttpGet("subdominio/{subdominioId:int}")]
        [Authorize]
        public async Task<ActionResult<object>> ObtenerEstadisticasSubdominio(int subdominioId, CancellationToken ct)
        {
            try
            {
                var estadisticas = await _actividadService.ObtenerEstadisticasPorSubdominioAsync(subdominioId);
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        /// <summary>
        /// Estadísticas del sistema de gestión documental:
        /// totales por estado y tipo, documentos vencidos y próximos a vencer.
        /// </summary>
        [HttpGet("documentos")]
        [Authorize]
        public async Task<ActionResult<object>> ObtenerEstadisticasDocumentos(CancellationToken ct)
        {
            try
            {
                var estadisticas = await _documentoService.ObtenerEstadisticasDocumentosAsync();
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        private static object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                return property?.GetValue(obj) ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
