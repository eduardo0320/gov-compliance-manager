using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IDashboardRepository dashboardRepository,
            ILogger<DashboardService> logger)
        {
            _dashboardRepository = dashboardRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<object>> ObtenerArbolCompletoAsync()
        {
            try
            {
                var dominios = await _dashboardRepository.ObtenerArbolCompletoAsync();

                return dominios.Select(d => new
                {
                    id     = d.IdDominio,
                    nombre = d.Nombre,
                    procesos = d.Procesos.OrderBy(p => p.Codigo).Select(p => new
                    {
                        idProceso                     = p.IdProceso,
                        codigo                        = p.Codigo,
                        nombre                        = p.Nombre,
                        marcoNormativo                = p.MarcoNormativo,
                        estadoImplementacion          = p.EstadoImplementacion,
                        porcentajeAvance              = p.PorcentajeAvance,
                        prioridadImplementacion       = p.PrioridadImplementacion,
                        fechaConclusionImplementacion = p.FechaConclusionImplementacion,
                        subdominios = p.Subdominios.OrderBy(s => s.IdSubdominio).Select(s => new
                        {
                            idSubdominio      = s.IdSubdominio,
                            practicasGobierno = s.PracticasGobierno,
                            actividades = s.Actividades.OrderBy(a => a.IdActividad).Select(a => new
                            {
                                idActividad          = a.IdActividad,
                                nombre               = a.Nombre,
                                estadoImplementacion = a.EstadoImplementacion,
                                porcentajeAvance     = a.PorcentajeAvance,
                                implementable        = a.Implementable,
                                fechaCompromiso      = a.FechaCompromiso,
                            })
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el árbol completo del dashboard");
                throw;
            }
        }

        public async Task<object> ObtenerStatsAsync()
        {
            try
            {
                var dominios = await _dashboardRepository.ObtenerArbolCompletoAsync();
                var lista = dominios.ToList();

                int totalProcesos    = 0;
                int totalSubdominios = 0;
                int totalActividades = 0;

                foreach (var dom in lista)
                {
                    totalProcesos += dom.Procesos.Count;
                    foreach (var proc in dom.Procesos)
                    {
                        totalSubdominios += proc.Subdominios.Count;
                        foreach (var sub in proc.Subdominios)
                            totalActividades += sub.Actividades.Count;
                    }
                }

                return new
                {
                    totalDominios    = lista.Count,
                    totalProcesos,
                    totalSubdominios,
                    totalActividades
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener stats del dashboard");
                throw;
            }
        }
    }
}
