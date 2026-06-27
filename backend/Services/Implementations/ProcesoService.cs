using backend.Models;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Services.Implementations
{
    public class ProcesoService : IProcesoService
    {
        private readonly IProcesoRepository _procesoRepository;
        private readonly IDominioRepository _dominioRepository;
        private readonly ISubdominioRepository _subdominioRepository;
        private readonly IActividadRepository _actividadRepository;
        private readonly ILogger<ProcesoService> _logger;

        public ProcesoService(
            IProcesoRepository procesoRepository,
            IDominioRepository dominioRepository,
            ISubdominioRepository subdominioRepository,
            IActividadRepository actividadRepository,
            ILogger<ProcesoService> logger)
        {
            _procesoRepository = procesoRepository;
            _dominioRepository = dominioRepository;
            _subdominioRepository = subdominioRepository;
            _actividadRepository = actividadRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<object>> ObtenerTodosLosProcesosAsync()
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                var dominios = await _dominioRepository.ObtenerTodos();

                return procesos.Select(p => new
                {
                    id = p.IdProceso,
                    codigo = p.Codigo,
                    nombre = p.Nombre,
                    prioridad_implementacion = p.PrioridadImplementacion,
                    marco_normativo = p.MarcoNormativo,
                    estado_implementacion = p.EstadoImplementacion,
                    porcentaje_avance = p.PorcentajeAvance,
                    fechaCreacion = p.FechaCreacion,
                    fecha_modificacion = p.FechaModificacion,
                    dominio = new
                    {
                        id = p.DominioId,
                        nombre = dominios.FirstOrDefault(d => d.IdDominio == p.DominioId)?.Nombre ?? "Sin dominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los procesos");
                throw;
            }
        }

        public async Task<object?> ObtenerProcesoPorIdAsync(int id)
        {
            try
            {
                var proceso = await _procesoRepository.ObtenerPorId(id);
                if (proceso == null) return null;

                var dominio = await _dominioRepository.ObtenerPorId(proceso.DominioId);
                var subdominios = await _procesoRepository.ObtenerSubdominiosPorIdProceso(id);

                return new
                {
                    id = proceso.IdProceso,
                    codigo = proceso.Codigo,
                    prioridad_implementacion = proceso.PrioridadImplementacion,
                    nombre = proceso.Nombre,
                    marco_normativo = proceso.MarcoNormativo,
                    estado_implementacion = proceso.EstadoImplementacion,
                    porcentaje_avance = proceso.PorcentajeAvance,
                    fechaCreacion = proceso.FechaCreacion,
                    fecha_modificacion = proceso.FechaModificacion,
                    creado_por_id = proceso.CreadoPorId,
                    modificado_por_id = proceso.ModificadoPorId,
                    dominio = new
                    {
                        id = proceso.DominioId,
                        nombre = dominio?.Nombre ?? "Sin dominio"
                    },
                    subdominios = subdominios.Select(s => new
                    {
                        idSubdominio = s.IdSubdominio,
                        practicasGobierno = s.PracticasGobierno,
                        indicadoresAsociados = s.IndicadoresAsociados,
                        procesoId = s.ProcesoId
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proceso por ID: {Id}", id);
                throw;
            }
        }

        public async Task<string> CrearProcesoAsync(string codigo, string nombre, string marcoNormativo, int dominioId, int creadoPorId, int? prioridadImplementacion = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    return "Error: El código del proceso es requerido";

                if (string.IsNullOrWhiteSpace(nombre))
                    return "Error: El nombre del proceso es requerido";

                // Permitir que el marco normativo sea vacío al actualizar (no bloquear la edición por esto)

                var dominio = await _dominioRepository.ObtenerPorId(dominioId);
                if (dominio == null)
                    return "Error: El dominio especificado no existe";

                if (await ExisteProcesoPorCodigoAsync(codigo))
                    return $"Error: Ya existe un proceso con el código '{codigo}'";

                if (await ExisteProcesoPorNombreYDominioAsync(nombre, dominioId))
                    return $"Error: Ya existe un proceso con el nombre '{nombre}' en este dominio";

                var proceso = new Proceso
                {
                    Codigo = codigo.Trim(),
                    Nombre = nombre.Trim(),
                    MarcoNormativo = marcoNormativo.Trim(),
                    EstadoImplementacion = "Sí", // Valor por defecto
                    PrioridadImplementacion = prioridadImplementacion ?? 0,
                    PorcentajeAvance = 0.00m,
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow,
                    CreadoPorId = creadoPorId,
                    ModificadoPorId = creadoPorId,
                    DominioId = dominioId
                };

                await _procesoRepository.Agregar(proceso);
                await _procesoRepository.GuardarCambios();

                _logger.LogInformation("Proceso creado exitosamente: {Codigo} - {Nombre} - ID: {Id}", codigo, nombre, proceso.IdProceso);
                return $"Proceso creado exitosamente: {proceso.IdProceso}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear proceso: {Codigo} - {Nombre}", codigo, nombre);
                return $"Error interno al crear el proceso: {ex.Message}";
            }
        }

        public async Task<string> ActualizarProcesoAsync(int id, string codigo, string nombre, string marcoNormativo, int dominioId, int modificadoPorId, int? prioridadImplementacion = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    return "Error: El código del proceso es requerido";

                if (string.IsNullOrWhiteSpace(nombre))
                    return "Error: El nombre del proceso es requerido";

                if (string.IsNullOrWhiteSpace(marcoNormativo))
                    return "Error: El marco normativo es requerido";

                var proceso = await _procesoRepository.ObtenerPorId(id);
                if (proceso == null)
                    return "Error: No se encontró el proceso especificado";

                var dominio = await _dominioRepository.ObtenerPorId(dominioId);
                if (dominio == null)
                    return "Error: El dominio especificado no existe";

                // Verificar código único (excluyendo el proceso actual)
                var procesoExistenteCodigo = await ObtenerProcesoPorCodigoAsync(codigo);
                if (procesoExistenteCodigo != null)
                {
                    var existenteData = procesoExistenteCodigo as dynamic;
                    if (existenteData?.id != id)
                        return $"Error: Ya existe otro proceso con el código '{codigo}'";
                }

                // Verificar nombre único en el dominio (excluyendo el proceso actual)
                if (proceso.Nombre != nombre || proceso.DominioId != dominioId)
                {
                    if (await ExisteProcesoPorNombreYDominioAsync(nombre, dominioId))
                    {
                        var procesos = await _procesoRepository.ObtenerTodos();
                        var procesoExistente = procesos.FirstOrDefault(p =>
                            p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
                            p.DominioId == dominioId);

                        if (procesoExistente != null && procesoExistente.IdProceso != id)
                            return $"Error: Ya existe otro proceso con el nombre '{nombre}' en este dominio";
                    }
                }

                proceso.Codigo = codigo.Trim();
                proceso.Nombre = nombre.Trim();
                proceso.MarcoNormativo = marcoNormativo.Trim();
                if (prioridadImplementacion.HasValue)
                {
                    proceso.PrioridadImplementacion = prioridadImplementacion.Value;
                }
                proceso.DominioId = dominioId;
                proceso.ModificadoPorId = modificadoPorId;
                proceso.FechaModificacion = DateTime.UtcNow;

                await _procesoRepository.Actualizar(proceso);
                await _procesoRepository.GuardarCambios();

                _logger.LogInformation("Proceso actualizado exitosamente: ID {Id}, Código {Codigo}", id, codigo);
                return "Proceso actualizado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar proceso: ID {Id}", id);
                return $"Error interno al actualizar el proceso: {ex.Message}";
            }
        }

        public async Task<string> EliminarProcesoAsync(int id)
        {
            try
            {
                var proceso = await _procesoRepository.ObtenerPorId(id);
                if (proceso == null)
                    return "Error: No se encontró el proceso especificado";

                if (await TieneSubdominiosAsociadosAsync(id))
                    return "Error: No se puede eliminar el proceso porque tiene subdominios asociados";

                await _procesoRepository.Eliminar(id);
                await _procesoRepository.GuardarCambios();

                _logger.LogInformation("Proceso eliminado exitosamente: ID {Id}", id);
                return "Proceso eliminado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar proceso: ID {Id}", id);
                return $"Error interno al eliminar el proceso: {ex.Message}";
            }
        }

        public async Task<bool> ExisteProcesoPorCodigoAsync(string codigo)
        {
            try
            {
                var proceso = await ObtenerProcesoPorCodigoAsync(codigo);
                return proceso != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de proceso por código: {Codigo}", codigo);
                return false;
            }
        }
        public async Task<bool> ExisteProcesoPorNombreYDominioAsync(string nombre, int dominioId)
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                return procesos.Any(p =>
                    p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
                    p.DominioId == dominioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de proceso por nombre y dominio: {Nombre}, {DominioId}", nombre, dominioId);
                return false;
            }
        }

        public async Task<object?> ObtenerProcesoPorCodigoAsync(string codigo)
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                var proceso = procesos.FirstOrDefault(p => p.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));

                if (proceso == null) return null;

                var dominio = await _dominioRepository.ObtenerPorId(proceso.DominioId);

                return new
                {
                    id = proceso.IdProceso,
                    codigo = proceso.Codigo,
                    nombre = proceso.Nombre,
                    marco_normativo = proceso.MarcoNormativo,
                    estado_implementacion = proceso.EstadoImplementacion,
                    porcentaje_avance = proceso.PorcentajeAvance,
                    dominio = new
                    {
                        id = proceso.DominioId,
                        nombre = dominio?.Nombre ?? "Sin dominio"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proceso por código: {Codigo}", codigo);
                return null;
            }
        }

        public async Task<IEnumerable<object>> BuscarProcesosPorNombreAsync(string nombre)
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                var procesosFiltrados = procesos.Where(p =>
                    p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));

                var dominios = await _dominioRepository.ObtenerTodos();

                return procesosFiltrados.Select(p => new
                {
                    id = p.IdProceso,
                    codigo = p.Codigo,
                    nombre = p.Nombre,
                    prioridad_implementacion = p.PrioridadImplementacion,
                    marco_normativo = p.MarcoNormativo,
                    estado_implementacion = p.EstadoImplementacion,
                    porcentaje_avance = p.PorcentajeAvance,
                    dominio = new
                    {
                        id = p.DominioId,
                        nombre = dominios.FirstOrDefault(d => d.IdDominio == p.DominioId)?.Nombre ?? "Sin dominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar procesos por nombre: {Nombre}", nombre);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerProcesosPorDominioAsync(int dominioId)
        {
            try
            {
                // EncontrarPorIdDominio ya hace Include(Dominio) e Include(Subdominios)
                var procesosDominio = await _procesoRepository.EncontrarPorIdDominio(dominioId);

                return procesosDominio.Select(p => (object)new
                {
                    id = p.IdProceso,
                    codigo = p.Codigo,
                    nombre = p.Nombre,
                    prioridad_implementacion = p.PrioridadImplementacion,
                    marco_normativo = p.MarcoNormativo,
                    estado_implementacion = p.EstadoImplementacion,
                    porcentaje_avance = p.PorcentajeAvance,
                    fechaCreacion = p.FechaCreacion,
                    dominio = new
                    {
                        id = p.DominioId,
                        nombre = p.Dominio?.Nombre ?? "Sin dominio"
                    },
                    subdominios = (p.Subdominios ?? new List<Subdominio>()).Select(s => new
                    {
                        idSubdominio = s.IdSubdominio,
                        practicasGobierno = s.PracticasGobierno,
                        indicadoresAsociados = s.IndicadoresAsociados,
                        procesoId = s.ProcesoId
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener procesos por dominio: {DominioId}", dominioId);
                return new List<object>();
            }
        }

        public async Task<string> ActualizarEstadoImplementacionAsync(int id, string estado)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(estado))
                    return "Error: El estado de implementación es requerido";

                if (!new[] { "Sí", "No" }.Contains(estado))
                    return "Error: El estado de implementación debe ser 'Sí' o 'No'";

                var proceso = await _procesoRepository.ObtenerPorId(id);
                if (proceso == null)
                    return "Error: No se encontró el proceso especificado";

                proceso.EstadoImplementacion = estado;
                proceso.FechaModificacion = DateTime.UtcNow;

                await _procesoRepository.Actualizar(proceso);
                await _procesoRepository.GuardarCambios();

                _logger.LogInformation("Estado de implementación actualizado: ID {Id}, Estado {Estado}", id, estado);
                return "Estado de implementación actualizado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado de implementación: ID {Id}", id);
                return $"Error interno al actualizar el estado: {ex.Message}";
            }
        }

        public async Task<string> ActualizarPorcentajeAvanceAsync(int id, decimal porcentaje)
        {
            try
            {
                if (porcentaje < 0 || porcentaje > 100)
                    return "Error: El porcentaje de avance debe estar entre 0 y 100";

                var proceso = await _procesoRepository.ObtenerPorId(id);
                if (proceso == null)
                    return "Error: No se encontró el proceso especificado";

                proceso.PorcentajeAvance = porcentaje;
                proceso.FechaModificacion = DateTime.UtcNow;

                await _procesoRepository.Actualizar(proceso);
                await _procesoRepository.GuardarCambios();

                _logger.LogInformation("Porcentaje de avance actualizado: ID {Id}, Porcentaje {Porcentaje}", id, porcentaje);
                return "Porcentaje de avance actualizado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar porcentaje de avance: ID {Id}", id);
                return $"Error interno al actualizar el porcentaje: {ex.Message}";
            }
        }

        public async Task<bool> TieneSubdominiosAsociadosAsync(int procesoId)
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                return subdominios.Any(s => s.ProcesoId == procesoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar subdominios asociados al proceso: {ProcesoId}", procesoId);
                return false;
            }
        }

        public async Task<IEnumerable<object>> ObtenerProcesosConSubdominiosAsync()
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                var dominios = await _dominioRepository.ObtenerTodos();
                var subdominios = await _subdominioRepository.ObtenerTodos();

                return procesos.Select(p => new
                {
                    id = p.IdProceso,
                    codigo = p.Codigo,
                    nombre = p.Nombre,
                    estado_implementacion = p.EstadoImplementacion,
                    porcentaje_avance = p.PorcentajeAvance,
                    dominio = new
                    {
                        id = p.DominioId,
                        nombre = dominios.FirstOrDefault(d => d.IdDominio == p.DominioId)?.Nombre ?? "Sin dominio"
                    },
                    subdominios = subdominios.Where(s => s.ProcesoId == p.IdProceso).Select(s => new
                    {
                        id = s.IdSubdominio,
                        practicas_gobierno = s.PracticasGobierno,
                        indicadores_asociados = s.IndicadoresAsociados
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener procesos con subdominios");
                return new List<object>();
            }
        }

        public async Task<object?> ObtenerProcesoConDetalleCompletoAsync(int id)
        {
            try
            {
                var proceso = await _procesoRepository.ObtenerPorId(id);
                if (proceso == null) return null;

                var dominio = await _dominioRepository.ObtenerPorId(proceso.DominioId);
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var subdominiosProceso = subdominios.Where(s => s.ProcesoId == id).ToList();

                // Obtener actividades de los subdominios
                var actividades = await _actividadRepository.ObtenerTodos();
                var subdominiosDetalle = new List<object>();

                foreach (var subdominio in subdominiosProceso)
                {
                    var actividadesSubdominio = actividades.Where(a => a.SubdominioId == subdominio.IdSubdominio);

                    subdominiosDetalle.Add(new
                    {
                        id = subdominio.IdSubdominio,
                        practicas_gobierno = subdominio.PracticasGobierno,
                        indicadores_asociados = subdominio.IndicadoresAsociados,
                        cantidad_actividades = actividadesSubdominio.Count(),
                        actividades = actividadesSubdominio.Select(a => new
                        {
                            id = a.IdActividad,
                            nombre = a.Nombre,
                            implementable = a.Implementable,
                            estado_implementacion = a.EstadoImplementacion,
                            porcentaje_avance = a.PorcentajeAvance
                        })
                    });
                }

                return new
                {
                    id = proceso.IdProceso,
                    codigo = proceso.Codigo,
                    nombre = proceso.Nombre,
                    marco_normativo = proceso.MarcoNormativo,
                    estado_implementacion = proceso.EstadoImplementacion,
                    porcentaje_avance = proceso.PorcentajeAvance,
                    fechaCreacion = proceso.FechaCreacion,
                    fecha_modificacion = proceso.FechaModificacion,
                    dominio = new
                    {
                        id = proceso.DominioId,
                        nombre = dominio?.Nombre ?? "Sin dominio"
                    },
                    cantidad_subdominios = subdominiosProceso.Count,
                    subdominios = subdominiosDetalle
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proceso con detalle completo: {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<object>> ObtenerProcesosConActividadesAsync()
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                var dominios = await _dominioRepository.ObtenerTodos();
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var actividades = await _actividadRepository.ObtenerTodos();

                return procesos.Select(p => new
                {
                    id = p.IdProceso,
                    codigo = p.Codigo,
                    nombre = p.Nombre,
                    estado_implementacion = p.EstadoImplementacion,
                    porcentaje_avance = p.PorcentajeAvance,
                    dominio = new
                    {
                        id = p.DominioId,
                        nombre = dominios.FirstOrDefault(d => d.IdDominio == p.DominioId)?.Nombre ?? "Sin dominio"
                    },
                    cantidad_actividades = ContarActividadesPorProcesoSync(p.IdProceso, subdominios, actividades),
                    actividades = ObtenerActividadesDelProcesoSync(p.IdProceso, subdominios, actividades)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener procesos con actividades");
                return new List<object>();
            }
        }

        private int ContarActividadesPorProcesoSync(int procesoId, IEnumerable<Subdominio> subdominios, IEnumerable<Actividad> actividades)
        {
            var subdominioIds = subdominios.Where(s => s.ProcesoId == procesoId).Select(s => s.IdSubdominio);
            return actividades.Count(a => subdominioIds.Contains(a.SubdominioId));
        }

        private IEnumerable<object> ObtenerActividadesDelProcesoSync(int procesoId, IEnumerable<Subdominio> subdominios, IEnumerable<Actividad> actividades)
        {
            var subdominioIds = subdominios.Where(s => s.ProcesoId == procesoId).Select(s => s.IdSubdominio);
            var actividadesProceso = actividades.Where(a => subdominioIds.Contains(a.SubdominioId));

            return actividadesProceso.Select(a => new
            {
                id = a.IdActividad,
                nombre = a.Nombre,
                implementable = a.Implementable,
                estado_implementacion = a.EstadoImplementacion,
                porcentaje_avance = a.PorcentajeAvance,
                subdominio = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
            });
        }

        public async Task<int> ContarSubdominiosPorProcesoAsync(int procesoId)
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                return subdominios.Count(s => s.ProcesoId == procesoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al contar subdominios del proceso: {ProcesoId}", procesoId);
                return 0;
            }
        }

        public async Task<int> ContarActividadesPorProcesoAsync(int procesoId)
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var actividades = await _actividadRepository.ObtenerTodos();

                var subdominioIds = subdominios.Where(s => s.ProcesoId == procesoId).Select(s => s.IdSubdominio);
                return actividades.Count(a => subdominioIds.Contains(a.SubdominioId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al contar actividades del proceso: {ProcesoId}", procesoId);
                return 0;
            }
        }

        public async Task<IEnumerable<object>> FiltrarProcesosPorEstadoAsync(string estado)
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                var procesosFiltrados = procesos.Where(p =>
                    p.EstadoImplementacion.Equals(estado, StringComparison.OrdinalIgnoreCase));

                var dominios = await _dominioRepository.ObtenerTodos();

                return procesosFiltrados.Select(p => new
                {
                    id = p.IdProceso,
                    codigo = p.Codigo,
                    nombre = p.Nombre,
                    marco_normativo = p.MarcoNormativo,
                    estado_implementacion = p.EstadoImplementacion,
                    porcentaje_avance = p.PorcentajeAvance,
                    dominio = new
                    {
                        id = p.DominioId,
                        nombre = dominios.FirstOrDefault(d => d.IdDominio == p.DominioId)?.Nombre ?? "Sin dominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al filtrar procesos por estado: {Estado}", estado);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerProcesosPorRangoAvanceAsync(decimal minPorcentaje, decimal maxPorcentaje)
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                var procesosFiltrados = procesos.Where(p =>
                    p.PorcentajeAvance >= minPorcentaje && p.PorcentajeAvance <= maxPorcentaje);

                var dominios = await _dominioRepository.ObtenerTodos();

                return procesosFiltrados.Select(p => new
                {
                    id = p.IdProceso,
                    codigo = p.Codigo,
                    nombre = p.Nombre,
                    estado_implementacion = p.EstadoImplementacion,
                    porcentaje_avance = p.PorcentajeAvance,
                    dominio = new
                    {
                        id = p.DominioId,
                        nombre = dominios.FirstOrDefault(d => d.IdDominio == p.DominioId)?.Nombre ?? "Sin dominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener procesos por rango de avance: {Min}-{Max}", minPorcentaje, maxPorcentaje);
                return new List<object>();
            }
        }

        private static string EstadoPorPorcentaje(decimal p)
        {
            if (p <= 0) return "Pendiente";
            if (p >= 100) return "Implementado";
            return "En Progreso";
        }

        public async Task<string> ActualizarPorcentajeActividadAsync(int actividadId, decimal porcentaje, int usuarioId)
        {
            if (porcentaje < 0 || porcentaje > 100)
                return "Error: El porcentaje de avance debe estar entre 0 y 100";

            var actividad = (await _actividadRepository.ObtenerTodos())
                .FirstOrDefault(a => a.IdActividad == actividadId);
            if (actividad == null)
                return "Error: No se encontró la actividad especificada";

            // 1) Actualiza la ACTIVIDAD
            actividad.PorcentajeAvance = porcentaje;
            actividad.EstadoImplementacion = EstadoPorPorcentaje(porcentaje);
            actividad.FechaControl = DateTime.UtcNow;
            await _actividadRepository.Actualizar(actividad);
            await _actividadRepository.GuardarCambios();

            // 2) Recalcular porcentaje del proceso que contiene esta actividad
            try
            {
                var subdominio = await _subdominioRepository.ObtenerPorId(actividad.SubdominioId);
                if (subdominio != null)
                {
                    var proceso = await _procesoRepository.ObtenerPorId(subdominio.ProcesoId);
                    if (proceso != null)
                    {
                        var allSubdominios = await _subdominioRepository.ObtenerTodos();
                        var subIds = allSubdominios.Where(s => s.ProcesoId == proceso.IdProceso).Select(s => s.IdSubdominio).ToList();

                        var allActividades = await _actividadRepository.ObtenerTodos();
                        var actividadesProceso = allActividades.Where(a => subIds.Contains(a.SubdominioId)).ToList();

                        decimal nuevoPorcentaje = 0m;
                        if (actividadesProceso.Any())
                        {
                            nuevoPorcentaje = actividadesProceso.Average(a => a.PorcentajeAvance);
                        }

                        proceso.PorcentajeAvance = Math.Round(nuevoPorcentaje, 2);
                        await _procesoRepository.Actualizar(proceso);
                        await _procesoRepository.GuardarCambios();
                        _logger.LogInformation("Porcentaje de proceso recalculado: ProcesoId {Id}, Porcentaje {Porcentaje}%", proceso.IdProceso, proceso.PorcentajeAvance);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recalcular porcentaje del proceso después de actualizar actividad ID {ActividadId}", actividadId);
            }

            return "Porcentaje de la actividad actualizado y proceso recalculado exitosamente";
        }
    }
}