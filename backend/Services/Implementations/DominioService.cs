using backend.Models;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Services.Implementations
{
    public class DominioService : IDominioService
    {
        private readonly IDominioRepository _dominioRepository;
        private readonly IProcesoRepository _procesoRepository;
        private readonly ISubdominioRepository _subdominioRepository;
        private readonly ILogger<DominioService> _logger;

        public DominioService(
            IDominioRepository dominioRepository,
            IProcesoRepository procesoRepository,
            ISubdominioRepository subdominioRepository,
            ILogger<DominioService> logger)
        {
            _dominioRepository = dominioRepository;
            _procesoRepository = procesoRepository;
            _subdominioRepository = subdominioRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<object>> ObtenerTodosLosDominiosAsync()
        {
            try
            {
                var dominios = await _dominioRepository.ObtenerTodos();
                return dominios.Select(d => new
                {
                    id = d.IdDominio,
                    nombre = d.Nombre,
                    cantidad_procesos = d.Procesos?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los dominios");
                throw;
            }
        }

        public async Task<object?> ObtenerDominioPorIdAsync(int id)
        {
            try
            {
                var dominio = await _dominioRepository.ObtenerPorId(id);
                if (dominio == null) return null;

                return new
                {
                    id = dominio.IdDominio,
                    nombre = dominio.Nombre,
                    cantidad_procesos = dominio.Procesos?.Count ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dominio por ID: {Id}", id);
                throw;
            }
        }

        public async Task<string> CrearDominioAsync(string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    return "Error: El nombre del dominio es requerido";

                if (await ExisteDominioPorNombreAsync(nombre))
                    return $"Error: Ya existe un dominio con el nombre '{nombre}'";

                var dominio = new Dominio
                {
                    Nombre = nombre.Trim()
                };

                await _dominioRepository.Agregar(dominio);
                await _dominioRepository.GuardarCambios();

                _logger.LogInformation("Dominio creado exitosamente: {Nombre}", nombre);
                return "Dominio creado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear dominio: {Nombre}", nombre);
                return $"Error interno al crear el dominio: {ex.Message}";
            }
        }

        public async Task<string> ActualizarDominioAsync(int id, string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    return "Error: El nombre del dominio es requerido";

                var dominio = await _dominioRepository.ObtenerPorId(id);
                if (dominio == null)
                    return "Error: No se encontró el dominio especificado";

                // Verificar si ya existe otro dominio con el mismo nombre
                var dominioExistente = await ObtenerDominioPorNombreAsync(nombre);
                if (dominioExistente != null)
                {
                    var existenteData = dominioExistente as dynamic;
                    if (existenteData?.id != id)
                        return $"Error: Ya existe otro dominio con el nombre '{nombre}'";
                }

                dominio.Nombre = nombre.Trim();
                await _dominioRepository.Actualizar(dominio);
                await _dominioRepository.GuardarCambios();

                _logger.LogInformation("Dominio actualizado exitosamente: ID {Id}, Nombre {Nombre}", id, nombre);
                return "Dominio actualizado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar dominio: ID {Id}, Nombre {Nombre}", id, nombre);
                return $"Error interno al actualizar el dominio: {ex.Message}";
            }
        }

        public async Task<string> EliminarDominioAsync(int id)
        {
            try
            {
                var dominio = await _dominioRepository.ObtenerPorId(id);
                if (dominio == null)
                    return "Error: No se encontró el dominio especificado";

                if (await TieneProcesosAsociadosAsync(id))
                    return "Error: No se puede eliminar el dominio porque tiene procesos asociados";

                await _dominioRepository.Eliminar(id);
                await _dominioRepository.GuardarCambios();

                _logger.LogInformation("Dominio eliminado exitosamente: ID {Id}", id);
                return "Dominio eliminado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar dominio: ID {Id}", id);
                return $"Error interno al eliminar el dominio: {ex.Message}";
            }
        }

        public async Task<bool> ExisteDominioPorNombreAsync(string nombre)
        {
            try
            {
                var dominio = await ObtenerDominioPorNombreAsync(nombre);
                return dominio != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de dominio por nombre: {Nombre}", nombre);
                return false;
            }
        }

        public async Task<object?> ObtenerDominioPorNombreAsync(string nombre)
        {
            try
            {
                var dominios = await _dominioRepository.ObtenerTodos();
                var dominio = dominios.FirstOrDefault(d => d.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));

                if (dominio == null) return null;

                return new
                {
                    id = dominio.IdDominio,
                    nombre = dominio.Nombre,
                    cantidad_procesos = dominio.Procesos?.Count ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dominio por nombre: {Nombre}", nombre);
                return null;
            }
        }

        public async Task<IEnumerable<object>> BuscarDominiosPorNombreAsync(string nombre)
        {
            try
            {
                var dominios = await _dominioRepository.ObtenerTodos();
                var dominiosFiltrados = dominios.Where(d =>
                    d.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));

                return dominiosFiltrados.Select(d => new
                {
                    id = d.IdDominio,
                    nombre = d.Nombre,
                    cantidad_procesos = d.Procesos?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar dominios por nombre: {Nombre}", nombre);
                return new List<object>();
            }
        }

        public async Task<bool> TieneProcesosAsociadosAsync(int dominioId)
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                return procesos.Any(p => p.DominioId == dominioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar procesos asociados al dominio: {DominioId}", dominioId);
                return false;
            }
        }

        public async Task<IEnumerable<object>> ObtenerDominiosConProcesosAsync()
        {
            try
            {
                var dominios = await _dominioRepository.ObtenerTodos();

                var resultado = new List<object>();
                foreach (var dominio in dominios)
                {
                    var procesos = await _procesoRepository.ObtenerTodos();
                    var procesosDominio = procesos.Where(p => p.DominioId == dominio.IdDominio);

                    resultado.Add(new
                    {
                        id = dominio.IdDominio,
                        nombre = dominio.Nombre,
                        procesos = procesosDominio.Select(p => new
                        {
                            id = p.IdProceso,
                            codigo = p.Codigo,
                            nombre = p.Nombre
                        })
                    });
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dominios con procesos");
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerDominiosConSubdominiosAsync()
        {
            try
            {
                var dominios = await _dominioRepository.ObtenerTodos();

                var resultado = new List<object>();
                foreach (var dominio in dominios)
                {
                    var procesos = await _procesoRepository.ObtenerTodos();
                    var procesosDominio = procesos.Where(p => p.DominioId == dominio.IdDominio);

                    var subdominios = new List<object>();
                    foreach (var proceso in procesosDominio)
                    {
                        var subdominiosProceso = await _subdominioRepository.ObtenerTodos();
                        var subdominiosDelProceso = subdominiosProceso.Where(s => s.ProcesoId == proceso.IdProceso);

                        foreach (var subdominio in subdominiosDelProceso)
                        {
                            subdominios.Add(new
                            {
                                id = subdominio.IdSubdominio,
                                practicas_gobierno = subdominio.PracticasGobierno,
                                indicadores_asociados = subdominio.IndicadoresAsociados,
                                proceso_nombre = proceso.Nombre
                            });
                        }
                    }

                    resultado.Add(new
                    {
                        id = dominio.IdDominio,
                        nombre = dominio.Nombre,
                        subdominios = subdominios
                    });
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dominios con subdominios");
                return new List<object>();
            }
        }

        public async Task<object?> ObtenerDominioConDetalleCompletoAsync(int id)
        {
            try
            {
                var dominio = await _dominioRepository.ObtenerPorId(id);
                if (dominio == null) return null;

                var procesos = await _procesoRepository.ObtenerTodos();
                var procesosDominio = procesos.Where(p => p.DominioId == id).ToList();

                var procesosDetalle = new List<object>();
                foreach (var proceso in procesosDominio)
                {
                    var subdominios = await _subdominioRepository.ObtenerTodos();
                    var subdominiosProceso = subdominios.Where(s => s.ProcesoId == proceso.IdProceso);

                    procesosDetalle.Add(new
                    {
                        id = proceso.IdProceso,
                        codigo = proceso.Codigo,
                        nombre = proceso.Nombre,
                        marco_normativo = proceso.MarcoNormativo,
                        estado_implementacion = proceso.EstadoImplementacion,
                        porcentaje_avance = proceso.PorcentajeAvance,
                        subdominios = subdominiosProceso.Select(s => new
                        {
                            id = s.IdSubdominio,
                            practicas_gobierno = s.PracticasGobierno,
                            indicadores_asociados = s.IndicadoresAsociados
                        })
                    });
                }

                return new
                {
                    id = dominio.IdDominio,
                    nombre = dominio.Nombre,
                    cantidad_procesos = procesosDominio.Count,
                    procesos = procesosDetalle
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dominio con detalle completo: {Id}", id);
                return null;
            }
        }

        public async Task<int> ContarProcesosPorDominioAsync(int dominioId)
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                return procesos.Count(p => p.DominioId == dominioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al contar procesos del dominio: {DominioId}", dominioId);
                return 0;
            }
        }
    }
}
