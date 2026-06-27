using backend.Models;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Services.Implementations
{
    public class SubdominioService : ISubdominioService
    {
        private readonly ISubdominioRepository _subdominioRepository;
        private readonly IProcesoRepository _procesoRepository;
        private readonly IDominioRepository _dominioRepository;
        private readonly IActividadRepository _actividadRepository;
        private readonly ILogger<SubdominioService> _logger;

        public SubdominioService(
            ISubdominioRepository subdominioRepository,
            IProcesoRepository procesoRepository,
            IDominioRepository dominioRepository,
            IActividadRepository actividadRepository,
            ILogger<SubdominioService> logger)
        {
            _subdominioRepository = subdominioRepository;
            _procesoRepository = procesoRepository;
            _dominioRepository = dominioRepository;
            _actividadRepository = actividadRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<object>> ObtenerTodosLosSubdominiosAsync()
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var procesos = await _procesoRepository.ObtenerTodos();

                return subdominios.Select(s => {
                    var proceso = procesos.FirstOrDefault(p => p.IdProceso == s.ProcesoId);
                    return new {
                        id = s.IdSubdominio,
                        practicas_gobierno = s.PracticasGobierno,
                        indicadores_asociados = s.IndicadoresAsociados,
                        proceso = new {
                            id = s.ProcesoId,
                            nombre = proceso?.Nombre ?? "Sin proceso",
                            dominioId = proceso?.DominioId ?? 0
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los subdominios");
                throw;
            }
        }

        public async Task<object?> ObtenerSubdominioPorIdAsync(int id)
        {
            try
            {
                var subdominio = await _subdominioRepository.ObtenerPorId(id);
                if (subdominio == null) return null;

                var proceso = await _procesoRepository.ObtenerPorId(subdominio.ProcesoId);

                return new
                {
                    id = subdominio.IdSubdominio,
                    practicas_gobierno = subdominio.PracticasGobierno,
                    indicadores_asociados = subdominio.IndicadoresAsociados,
                    proceso = new
                    {
                        id = subdominio.ProcesoId,
                        nombre = proceso?.Nombre ?? "Sin proceso"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener subdominio por ID: {Id}", id);
                throw;
            }
        }

        public async Task<string> CrearSubdominioAsync(string practicasGobierno, string indicadoresAsociados, int procesoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(practicasGobierno))
                    return "Error: Las prácticas de gobierno son requeridas";

                var proceso = await _procesoRepository.ObtenerPorId(procesoId);
                if (proceso == null)
                    return "Error: El proceso especificado no existe";

                if (await ExisteSubdominioPorPracticasYProcesoAsync(practicasGobierno, procesoId))
                    return $"Error: Ya existe un subdominio con las mismas prácticas de gobierno en este proceso";

                var subdominio = new Subdominio
                {
                    PracticasGobierno = practicasGobierno.Trim(),
                    IndicadoresAsociados = indicadoresAsociados?.Trim() ?? string.Empty,
                    ProcesoId = procesoId
                };

                await _subdominioRepository.Agregar(subdominio);
                await _subdominioRepository.GuardarCambios();

                _logger.LogInformation("Subdominio creado exitosamente: {PracticasGobierno}", practicasGobierno);
                return "Subdominio creado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear subdominio: {PracticasGobierno}", practicasGobierno);
                return $"Error interno al crear el subdominio: {ex.Message}";
            }
        }

        public async Task<string> ActualizarSubdominioAsync(int id, string practicasGobierno, string indicadoresAsociados, int procesoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(practicasGobierno))
                    return "Error: Las prácticas de gobierno son requeridas";

                var subdominio = await _subdominioRepository.ObtenerPorId(id);
                if (subdominio == null)
                    return "Error: No se encontró el subdominio especificado";

                var proceso = await _procesoRepository.ObtenerPorId(procesoId);
                if (proceso == null)
                    return "Error: El proceso especificado no existe";

                // Verificar si ya existe otro subdominio con las mismas prácticas en el proceso
                if (subdominio.PracticasGobierno != practicasGobierno || subdominio.ProcesoId != procesoId)
                {
                    if (await ExisteSubdominioPorPracticasYProcesoAsync(practicasGobierno, procesoId))
                    {
                        var subdominios = await _subdominioRepository.ObtenerTodos();
                        var subdominioExistente = subdominios.FirstOrDefault(s =>
                            s.PracticasGobierno.Equals(practicasGobierno, StringComparison.OrdinalIgnoreCase) &&
                            s.ProcesoId == procesoId);

                        if (subdominioExistente != null && subdominioExistente.IdSubdominio != id)
                            return $"Error: Ya existe otro subdominio con las mismas prácticas de gobierno en este proceso";
                    }
                }

                subdominio.PracticasGobierno = practicasGobierno.Trim();
                subdominio.IndicadoresAsociados = indicadoresAsociados?.Trim() ?? string.Empty;
                subdominio.ProcesoId = procesoId;

                await _subdominioRepository.Actualizar(subdominio);
                await _subdominioRepository.GuardarCambios();

                _logger.LogInformation("Subdominio actualizado exitosamente: ID {Id}", id);
                return "Subdominio actualizado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar subdominio: ID {Id}", id);
                return $"Error interno al actualizar el subdominio: {ex.Message}";
            }
        }

        public async Task<string> EliminarSubdominioAsync(int id)
        {
            try
            {
                var subdominio = await _subdominioRepository.ObtenerPorId(id);
                if (subdominio == null)
                    return "Error: No se encontró el subdominio especificado";

                if (await TieneActividadesAsociadasAsync(id))
                    return "Error: No se puede eliminar el subdominio porque tiene actividades asociadas";

                await _subdominioRepository.Eliminar(id);
                await _subdominioRepository.GuardarCambios();

                _logger.LogInformation("Subdominio eliminado exitosamente: ID {Id}", id);
                return "Subdominio eliminado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar subdominio: ID {Id}", id);
                return $"Error interno al eliminar el subdominio: {ex.Message}";
            }
        }

        public async Task<bool> ExisteSubdominioPorPracticasYProcesoAsync(string practicasGobierno, int procesoId)
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                return subdominios.Any(s =>
                    s.PracticasGobierno.Equals(practicasGobierno, StringComparison.OrdinalIgnoreCase) &&
                    s.ProcesoId == procesoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de subdominio: {PracticasGobierno}, {ProcesoId}", practicasGobierno, procesoId);
                return false;
            }
        }

        public async Task<IEnumerable<object>> BuscarSubdominiosPorPracticasAsync(string practicas)
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var subdominiosFiltrados = subdominios.Where(s =>
                    s.PracticasGobierno.Contains(practicas, StringComparison.OrdinalIgnoreCase));

                var procesos = await _procesoRepository.ObtenerTodos();

                return subdominiosFiltrados.Select(s => new
                {
                    id = s.IdSubdominio,
                    practicas_gobierno = s.PracticasGobierno,
                    indicadores_asociados = s.IndicadoresAsociados,
                    proceso = new
                    {
                        id = s.ProcesoId,
                        nombre = procesos.FirstOrDefault(p => p.IdProceso == s.ProcesoId)?.Nombre ?? "Sin proceso"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar subdominios por prácticas: {Practicas}", practicas);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerSubdominiosPorProcesoAsync(int procesoId)
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var subdominiosProceso = subdominios.Where(s => s.ProcesoId == procesoId);

                var proceso = await _procesoRepository.ObtenerPorId(procesoId);

                return subdominiosProceso.Select(s => new
                {
                    id = s.IdSubdominio,
                    practicas_gobierno = s.PracticasGobierno,
                    indicadores_asociados = s.IndicadoresAsociados,
                    proceso = new
                    {
                        id = s.ProcesoId,
                        nombre = proceso?.Nombre ?? "Sin proceso"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener subdominios por proceso: {ProcesoId}", procesoId);
                return new List<object>();
            }
        }

        public async Task<bool> TieneActividadesAsociadasAsync(int subdominioId)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                return actividades.Any(a => a.SubdominioId == subdominioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar actividades asociadas al subdominio: {SubdominioId}", subdominioId);
                return false;
            }
        }

        public async Task<IEnumerable<object>> ObtenerSubdominiosConActividadesAsync()
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var procesos = await _procesoRepository.ObtenerTodos();
                var actividades = await _actividadRepository.ObtenerTodos();

                return subdominios.Select(s => new
                {
                    id = s.IdSubdominio,
                    practicas_gobierno = s.PracticasGobierno,
                    indicadores_asociados = s.IndicadoresAsociados,
                    proceso = new
                    {
                        id = s.ProcesoId,
                        nombre = procesos.FirstOrDefault(p => p.IdProceso == s.ProcesoId)?.Nombre ?? "Sin proceso"
                    },
                    actividades = actividades.Where(a => a.SubdominioId == s.IdSubdominio).Select(a => new
                    {
                        id = a.IdActividad,
                        nombre = a.Nombre,
                        implementable = a.Implementable,
                        estado_implementacion = a.EstadoImplementacion,
                        porcentaje_avance = a.PorcentajeAvance
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener subdominios con actividades");
                return new List<object>();
            }
        }

        public async Task<object?> ObtenerSubdominioConDetalleCompletoAsync(int id)
        {
            try
            {
                var subdominio = await _subdominioRepository.ObtenerPorId(id);
                if (subdominio == null) return null;

                var proceso = await _procesoRepository.ObtenerPorId(subdominio.ProcesoId);
                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesSubdominio = actividades.Where(a => a.SubdominioId == id).ToList();

                // Obtener dominio del proceso
                Dominio? dominio = null;
                if (proceso != null)
                {
                    dominio = await _dominioRepository.ObtenerPorId(proceso.DominioId);
                }

                return new
                {
                    id = subdominio.IdSubdominio,
                    practicas_gobierno = subdominio.PracticasGobierno,
                    indicadores_asociados = subdominio.IndicadoresAsociados,
                    proceso = new
                    {
                        id = subdominio.ProcesoId,
                        nombre = proceso?.Nombre ?? "Sin proceso",
                        codigo = proceso?.Codigo ?? "Sin código",
                        dominio = new
                        {
                            id = proceso?.DominioId ?? 0,
                            nombre = dominio?.Nombre ?? "Sin dominio"
                        }
                    },
                    cantidad_actividades = actividadesSubdominio.Count,
                    actividades = actividadesSubdominio.Select(a => new
                    {
                        id = a.IdActividad,
                        nombre = a.Nombre,
                        implementable = a.Implementable,
                        fecha_compromiso = a.FechaCompromiso,
                        estado_implementacion = a.EstadoImplementacion,
                        porcentaje_avance = a.PorcentajeAvance,
                        funcionarios_responsables_id = a.FuncionariosResponsablesId,
                        fecha_control = a.FechaControl,
                        documentos = a.Documentos,
                        observaciones = a.Observaciones
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener subdominio con detalle completo: {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<object>> ObtenerSubdominiosConProcesoYDominioAsync()
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var procesos = await _procesoRepository.ObtenerTodos();
                var dominios = await _dominioRepository.ObtenerTodos();

                return subdominios.Select(s => new
                {
                    id = s.IdSubdominio,
                    practicas_gobierno = s.PracticasGobierno,
                    indicadores_asociados = s.IndicadoresAsociados,
                    proceso = new
                    {
                        id = s.ProcesoId,
                        nombre = procesos.FirstOrDefault(p => p.IdProceso == s.ProcesoId)?.Nombre ?? "Sin proceso",
                        codigo = procesos.FirstOrDefault(p => p.IdProceso == s.ProcesoId)?.Codigo ?? "Sin código",
                        dominio = new
                        {
                            id = procesos.FirstOrDefault(p => p.IdProceso == s.ProcesoId)?.DominioId ?? 0,
                            nombre = dominios.FirstOrDefault(d => d.IdDominio ==
                                (procesos.FirstOrDefault(p => p.IdProceso == s.ProcesoId)?.DominioId ?? 0))?.Nombre ?? "Sin dominio"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener subdominios con proceso y dominio");
                return new List<object>();
            }
        }

        public async Task<int> ContarActividadesPorSubdominioAsync(int subdominioId)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                return actividades.Count(a => a.SubdominioId == subdominioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al contar actividades del subdominio: {SubdominioId}", subdominioId);
                return 0;
            }
        }

        public async Task<IEnumerable<object>> FiltrarSubdominiosPorIndicadoresAsync(string indicadores)
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var subdominiosFiltrados = subdominios.Where(s =>
                    s.IndicadoresAsociados.Contains(indicadores, StringComparison.OrdinalIgnoreCase));

                var procesos = await _procesoRepository.ObtenerTodos();

                return subdominiosFiltrados.Select(s => new
                {
                    id = s.IdSubdominio,
                    practicas_gobierno = s.PracticasGobierno,
                    indicadores_asociados = s.IndicadoresAsociados,
                    proceso = new
                    {
                        id = s.ProcesoId,
                        nombre = procesos.FirstOrDefault(p => p.IdProceso == s.ProcesoId)?.Nombre ?? "Sin proceso"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al filtrar subdominios por indicadores: {Indicadores}", indicadores);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerSubdominiosPorDominioAsync(int dominioId)
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                var procesosDominio = procesos.Where(p => p.DominioId == dominioId);
                var procesosIds = procesosDominio.Select(p => p.IdProceso);

                var subdominios = await _subdominioRepository.ObtenerTodos();
                var subdominiosDominio = subdominios.Where(s => procesosIds.Contains(s.ProcesoId));

                return subdominiosDominio.Select(s => new
                {
                    id = s.IdSubdominio,
                    practicas_gobierno = s.PracticasGobierno,
                    indicadores_asociados = s.IndicadoresAsociados,
                    proceso = new
                    {
                        id = s.ProcesoId,
                        nombre = procesos.FirstOrDefault(p => p.IdProceso == s.ProcesoId)?.Nombre ?? "Sin proceso"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener subdominios por dominio: {DominioId}", dominioId);
                return new List<object>();
            }
        }
    }
}