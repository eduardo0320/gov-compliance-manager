using backend.Models;
using System.Linq;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Services.Implementations
{
    public class ActividadService : IActividadService
    {
        private readonly IActividadRepository _actividadRepository;
        private readonly ISubdominioRepository _subdominioRepository;
        private readonly IProcesoRepository _procesoRepository;
        private readonly IDominioRepository _dominioRepository;
        private readonly IDocumentoRepository? _documentoRepository;
        private readonly INotificacionService _notificacionService;

        private readonly IEmailService _emailService;
        private readonly ILogger<ActividadService> _logger;

        private static bool TieneDocumentoPrincipalVencido(IEnumerable<Documento>? documentos)
        {
            var principal = documentos?.FirstOrDefault(d =>
                string.Equals(d.RolEnActividad, "Principal", StringComparison.OrdinalIgnoreCase));

            return principal?.FechaVencimiento.HasValue == true
                && principal.FechaVencimiento.Value.Date < DateTime.UtcNow.Date;
        }

        public ActividadService(
            IActividadRepository actividadRepository,
            ISubdominioRepository subdominioRepository,
            IProcesoRepository procesoRepository,
            IDominioRepository dominioRepository,
            INotificacionService notificacionService,
            IEmailService emailService,
            ILogger<ActividadService> logger,
            IDocumentoRepository? documentoRepository = null)
        {
            _actividadRepository = actividadRepository;
            _subdominioRepository = subdominioRepository;
            _procesoRepository = procesoRepository;
            _dominioRepository = dominioRepository;
            _documentoRepository = documentoRepository;
            _notificacionService = notificacionService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IEnumerable<object>> ObtenerTodasLasActividadesAsync()
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var subdominios = await _subdominioRepository.ObtenerTodos();

                var result = new List<object>();
                foreach (var a in actividades)
                {
                    bool tieneVencidos = false;
                    try
                    {
                        var docs = _documentoRepository != null ? await _documentoRepository.ObtenerPorActividadId(a.IdActividad) : null;
                        tieneVencidos = TieneDocumentoPrincipalVencido(docs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudieron comprobar documentos para actividad {Id}", a.IdActividad);
                    }

                    result.Add(new
                    {
                        id = a.IdActividad,
                        nombre = a.Nombre,
                        implementable = a.Implementable,
                        fecha_compromiso = a.FechaCompromiso,
                        estado_implementacion = a.EstadoImplementacion,
                        porcentaje_avance = a.PorcentajeAvance,
                        funcionarios_responsables_id = a.FuncionariosResponsablesId,
                        funcionarios_responsables_nombre = a.FuncionariosResponsables != null ? a.FuncionariosResponsables.nombre : null,
                        fecha_control = a.FechaControl,
                        documentos = a.Documentos,
                        observaciones = a.Observaciones,
                        tieneDocumentosVencidos = tieneVencidos,
                        subdominio = new
                        {
                            id = a.SubdominioId,
                            practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                        }
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las actividades");
                throw;
            }
        }

        public async Task<object?> ObtenerActividadPorIdAsync(int id)
        {
            try
            {
                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null) return null;

                var subdominio = await _subdominioRepository.ObtenerPorId(actividad.SubdominioId);

                Proceso? proceso = null;
                Dominio? dominio = null;

                if (subdominio != null)
                {
                    proceso = await _procesoRepository.ObtenerPorId(subdominio.ProcesoId);
                    if (proceso != null)
                        dominio = await _dominioRepository.ObtenerPorId(proceso.DominioId);
                }

                bool tieneVencidos = false;
                try
                {
                    var docs = _documentoRepository != null ? await _documentoRepository.ObtenerPorActividadId(actividad.IdActividad) : null;
                    tieneVencidos = TieneDocumentoPrincipalVencido(docs);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudieron comprobar documentos para actividad {Id}", actividad.IdActividad);
                }

                return new
                {
                    idActividad = actividad.IdActividad,
                    nombre = actividad.Nombre,
                    implementable = actividad.Implementable,
                    fechaCompromiso = actividad.FechaCompromiso,
                    estadoImplementacion = actividad.EstadoImplementacion,
                    porcentajeAvance = actividad.PorcentajeAvance,
                    funcionariosResponsablesId = actividad.FuncionariosResponsablesId,
                    fechaControl = actividad.FechaControl,
                    documentos = actividad.Documentos,
                    observaciones = actividad.Observaciones,
                    tieneDocumentosVencidos = tieneVencidos,
                    subdominio = new
                    {
                        id = actividad.SubdominioId,
                        practicasGobierno = subdominio?.PracticasGobierno ?? "Sin subdominio"
                    },
                    proceso = proceso == null ? null : new
                    {
                        id = proceso.IdProceso,
                        codigo = proceso.Codigo,
                        nombre = proceso.Nombre
                    },
                    dominio = dominio == null ? null : new
                    {
                        id = dominio.IdDominio,
                        nombre = dominio.Nombre
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividad por ID: {Id}", id);
                throw;
            }
        }

        public async Task<string> CrearActividadAsync(string nombre, string implementable, int funcionariosResponsablesId, int subdominioId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    return "Error: El nombre de la actividad es requerido";

                if (string.IsNullOrWhiteSpace(implementable) || (implementable != "Sí" && implementable != "No"))
                    return "Error: Implementable debe ser 'Sí' o 'No'";

                var subdominio = await _subdominioRepository.ObtenerPorId(subdominioId);
                if (subdominio == null)
                    return "Error: El subdominio especificado no existe";

                if (await ExisteActividadPorNombreYSubdominioAsync(nombre, subdominioId))
                    return $"Error: Ya existe una actividad con el mismo nombre en este subdominio";

                var actividad = new Actividad
                {
                    Nombre = nombre.Trim(),
                    Implementable = implementable,
                    EstadoImplementacion = "Pendiente",
                    PorcentajeAvance = 0.00m,
                    FuncionariosResponsablesId = funcionariosResponsablesId,
                    SubdominioId = subdominioId
                };

                await _actividadRepository.Agregar(actividad);
                await _actividadRepository.GuardarCambios();

                _logger.LogInformation("Actividad creada exitosamente: {Nombre} - ID: {Id}", nombre, actividad.IdActividad);

                // Notificar al funcionario responsable si está asignado
                if (funcionariosResponsablesId > 0)
                {
                    await _notificacionService.CrearNotificacionAsync(
                        funcionariosResponsablesId,
                        "Nueva actividad asignada",
                        $"Se te ha asignado la actividad \"{nombre.Trim()}\".",
                        "info",
                        $"/subdominios/{subdominioId}/actividades/{actividad.IdActividad}/editar"
                    );
                }
                // Recalcular porcentaje del proceso que contiene esta actividad
                try
                {
                    var sd = await _subdominioRepository.ObtenerPorId(actividad.SubdominioId);
                    if (sd != null)
                    {
                        var proceso = await _procesoRepository.ObtenerPorId(sd.ProcesoId);
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
                            _logger.LogInformation("Porcentaje de proceso recalculado (crear actividad): ProcesoId {Id}, Porcentaje {Porcentaje}%", proceso.IdProceso, proceso.PorcentajeAvance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al recalcular porcentaje del proceso después de crear actividad: {ActividadId}", actividad.IdActividad);
                }

                return $"Actividad creada exitosamente: {actividad.IdActividad}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear actividad: {Nombre}", nombre);
                return $"Error interno al crear la actividad: {ex.Message}";
            }
        }

        public async Task<string> ActualizarActividadAsync(int id, string nombre, string implementable, int? funcionariosResponsablesId, int subdominioId)
        {
            try
            {
                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null)
                    return "Error: No se encontró la actividad especificada";

                // Si el nombre está vacío, preservar el nombre actual
                string nombreActualizar = string.IsNullOrWhiteSpace(nombre) ? actividad.Nombre : nombre.Trim();

                if (string.IsNullOrWhiteSpace(implementable) || (implementable != "Sí" && implementable != "No"))
                    return "Error: Implementable debe ser 'Sí' o 'No'";

                var subdominio = await _subdominioRepository.ObtenerPorId(subdominioId);
                if (subdominio == null)
                    return "Error: El subdominio especificado no existe";

                // Verificar si ya existe otra actividad con el mismo nombre en el subdominio
                // (solo si el nombre cambió)
                if (actividad.Nombre != nombreActualizar || actividad.SubdominioId != subdominioId)
                {
                    if (await ExisteActividadPorNombreYSubdominioAsync(nombreActualizar, subdominioId))
                    {
                        var actividades = await _actividadRepository.ObtenerTodos();
                        var actividadExistente = actividades.FirstOrDefault(a =>
                            a.Nombre.Equals(nombreActualizar, StringComparison.OrdinalIgnoreCase) &&
                            a.SubdominioId == subdominioId);

                        if (actividadExistente != null && actividadExistente.IdActividad != id)
                            return $"Error: Ya existe otra actividad con el mismo nombre en este subdominio";
                    }
                }

                var responsableAnterior = actividad.FuncionariosResponsablesId;

                actividad.Nombre = nombreActualizar;
                actividad.Implementable = implementable;
                actividad.FuncionariosResponsablesId = funcionariosResponsablesId;
                actividad.SubdominioId = subdominioId;

                await _actividadRepository.Actualizar(actividad);
                await _actividadRepository.GuardarCambios();

                // Notificar si se reasignó a un nuevo responsable
                if (funcionariosResponsablesId.HasValue && funcionariosResponsablesId.Value > 0 && funcionariosResponsablesId != responsableAnterior)
                {
                    await _notificacionService.CrearNotificacionAsync(
                        funcionariosResponsablesId.Value,
                        "Nueva actividad asignada",
                        $"Se te ha asignado la actividad \"{nombreActualizar}\".",
                        "info",
                        $"/subdominios/{subdominioId}/actividades/{id}/editar"
                    );
                }

                _logger.LogInformation("Actividad actualizada correctamente: ID {Id}", id);
                return "Actividad actualizada correctamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar actividad: ID {Id}", id);
                return $"Error interno al actualizar la actividad: {ex.Message}";
            }
        }

        public async Task<string> EliminarActividadAsync(int id)
        {
            try
            {
                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null)
                    return "Error: No se encontró la actividad especificada";

                // guardar el subdominio antes de eliminar
                var subdominioId = actividad.SubdominioId;

                await _actividadRepository.Eliminar(id);
                await _actividadRepository.GuardarCambios();

                // Recalcular porcentaje del proceso asociado al subdominio
                try
                {
                    var subdominio = await _subdominioRepository.ObtenerPorId(subdominioId);
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
                            _logger.LogInformation("Porcentaje de proceso recalculado (eliminar actividad): ProcesoId {Id}, Porcentaje {Porcentaje}%", proceso.IdProceso, proceso.PorcentajeAvance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al recalcular porcentaje del proceso después de eliminar actividad ID {ActividadId}", id);
                }

                _logger.LogInformation("Actividad eliminada exitosamente: ID {Id}", id);
                return "Actividad eliminada exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar actividad: ID {Id}", id);
                return $"Error interno al eliminar la actividad: {ex.Message}";
            }
        }

        public async Task<bool> ExisteActividadPorNombreYSubdominioAsync(string nombre, int subdominioId)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                return actividades.Any(a =>
                    a.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
                    a.SubdominioId == subdominioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de actividad: {Nombre}, {SubdominioId}", nombre, subdominioId);
                return false;
            }
        }

        public async Task<IEnumerable<object>> BuscarActividadesPorNombreAsync(string nombre)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesFiltradas = actividades.Where(a =>
                    a.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));

                var subdominios = await _subdominioRepository.ObtenerTodos();

                return actividadesFiltradas.Select(a => new
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
                    observaciones = a.Observaciones,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar actividades por nombre: {Nombre}", nombre);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerActividadesPorSubdominioAsync(int subdominioId)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesSubdominio = actividades.Where(a => a.SubdominioId == subdominioId);

                var subdominio = await _subdominioRepository.ObtenerPorId(subdominioId);

                return actividadesSubdominio.Select(a => new
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
                    observaciones = a.Observaciones,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominio?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades por subdominio: {SubdominioId}", subdominioId);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerActividadesPorProcesoAsync(int procesoId)
        {
            try
            {
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var subdominiosProceso = subdominios.Where(s => s.ProcesoId == procesoId);
                var subdominiosIds = subdominiosProceso.Select(s => s.IdSubdominio);

                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesProceso = actividades.Where(a => subdominiosIds.Contains(a.SubdominioId));

                return actividadesProceso.Select(a => new
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
                    observaciones = a.Observaciones,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades por proceso: {ProcesoId}", procesoId);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerActividadesPorDominioAsync(int dominioId)
        {
            try
            {
                var procesos = await _procesoRepository.ObtenerTodos();
                var procesosDominio = procesos.Where(p => p.DominioId == dominioId);
                var procesosIds = procesosDominio.Select(p => p.IdProceso);

                var subdominios = await _subdominioRepository.ObtenerTodos();
                var subdominiosDominio = subdominios.Where(s => procesosIds.Contains(s.ProcesoId));
                var subdominiosIds = subdominiosDominio.Select(s => s.IdSubdominio);

                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesDominio = actividades.Where(a => subdominiosIds.Contains(a.SubdominioId));

                return actividadesDominio.Select(a => new
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
                    observaciones = a.Observaciones,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades por dominio: {DominioId}", dominioId);
                return new List<object>();
            }
        }

        public async Task<string> ActualizarEstadoImplementacionAsync(int id, string estado)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(estado))
                    return "Error: El estado de implementación es requerido";

                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null)
                    return "Error: No se encontró la actividad especificada";

                actividad.EstadoImplementacion = estado.Trim();

                await _actividadRepository.Actualizar(actividad);
                await _actividadRepository.GuardarCambios();

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

                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null)
                    return "Error: No se encontró la actividad especificada";

                actividad.PorcentajeAvance = porcentaje;

                // Actualizar automáticamente el estado basado en el porcentaje
                if (porcentaje == 0)
                    actividad.EstadoImplementacion = "Pendiente";
                else if (porcentaje < 100)
                    actividad.EstadoImplementacion = "En Progreso";
                else
                    actividad.EstadoImplementacion = "Implementado";

                await _actividadRepository.Actualizar(actividad);
                await _actividadRepository.GuardarCambios();

                // Recalcular porcentaje del proceso asociado
                try
                {
                    // Obtener el subdominio y desde ahí el proceso
                    var subdominio = await _subdominioRepository.ObtenerPorId(actividad.SubdominioId);
                    if (subdominio != null)
                    {
                        var proceso = await _procesoRepository.ObtenerPorId(subdominio.ProcesoId);
                        if (proceso != null)
                        {
                            // Obtener todas las actividades pertenecientes al proceso
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
                    _logger.LogError(ex, "Error al recalcular porcentaje del proceso después de actualizar actividad ID {Id}", id);
                }

                _logger.LogInformation("Porcentaje de avance actualizado: ID {Id}, Porcentaje {Porcentaje}%", id, porcentaje);
                return "Porcentaje de avance actualizado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar porcentaje de avance: ID {Id}", id);
                return $"Error interno al actualizar el porcentaje: {ex.Message}";
            }
        }

        public async Task<string> ActualizarFechaCompromisoAsync(int id, DateTime? fechaCompromiso)
        {
            try
            {
                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null)
                    return "Error: No se encontró la actividad especificada";

                actividad.FechaCompromiso = fechaCompromiso;

                await _actividadRepository.Actualizar(actividad);
                await _actividadRepository.GuardarCambios();

                _logger.LogInformation("Fecha compromiso actualizada: ID {Id}", id);
                return "Fecha compromiso actualizada exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar fecha compromiso: ID {Id}", id);
                return $"Error interno al actualizar la fecha compromiso: {ex.Message}";
            }
        }

        public async Task<string> ActualizarFechaControlAsync(int id, DateTime? fechaControl)
        {
            try
            {
                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null)
                    return "Error: No se encontró la actividad especificada";

                actividad.FechaControl = fechaControl;

                await _actividadRepository.Actualizar(actividad);
                await _actividadRepository.GuardarCambios();

                _logger.LogInformation("Fecha control actualizada: ID {Id}", id);
                return "Fecha control actualizada exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar fecha control: ID {Id}", id);
                return $"Error interno al actualizar la fecha control: {ex.Message}";
            }
        }

        public async Task<string> ActualizarDocumentosAsync(int id, string? documentos)
        {
            try
            {
                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null)
                    return "Error: No se encontró la actividad especificada";

                actividad.Documentos = documentos?.Trim();

                await _actividadRepository.Actualizar(actividad);
                await _actividadRepository.GuardarCambios();

                _logger.LogInformation("Documentos actualizados: ID {Id}", id);
                return "Documentos actualizados exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar documentos: ID {Id}", id);
                return $"Error interno al actualizar los documentos: {ex.Message}";
            }
        }

        public async Task<string> ActualizarObservacionesAsync(int id, string? observaciones)
        {
            try
            {
                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null)
                    return "Error: No se encontró la actividad especificada";

                actividad.Observaciones = observaciones?.Trim();

                await _actividadRepository.Actualizar(actividad);
                await _actividadRepository.GuardarCambios();

                _logger.LogInformation("Observaciones actualizadas: ID {Id}", id);
                return "Observaciones actualizadas exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar observaciones: ID {Id}", id);
                return $"Error interno al actualizar las observaciones: {ex.Message}";
            }
        }

        public async Task<IEnumerable<object>> ObtenerActividadesConDetalleCompletoAsync()
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var procesos = await _procesoRepository.ObtenerTodos();
                var dominios = await _dominioRepository.ObtenerTodos();

                return actividades.Select(a =>
                {
                    var subdominio = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId);
                    var proceso = subdominio != null ? procesos.FirstOrDefault(p => p.IdProceso == subdominio.ProcesoId) : null;
                    var dominio = proceso != null ? dominios.FirstOrDefault(d => d.IdDominio == proceso.DominioId) : null;

                    return new
                    {
                        id = a.IdActividad,
                        nombre = a.Nombre,
                        implementable = a.Implementable,
                        fecha_compromiso = a.FechaCompromiso,
                        estado_implementacion = a.EstadoImplementacion,
                        porcentaje_avance = a.PorcentajeAvance,
                        funcionarios_responsables_id = a.FuncionariosResponsablesId,
                        funcionarios_responsables_nombre = a.FuncionariosResponsables?.nombre ?? "Sin responsable",
                        funcionarios_responsables = new
                        {
                            id = a.FuncionariosResponsablesId,
                            nombre = a.FuncionariosResponsables?.nombre ?? "Sin responsable",
                            cedula = a.FuncionariosResponsables?.cedula ?? string.Empty,
                            correo_electronico = a.FuncionariosResponsables?.correo_electronico ?? string.Empty
                        },
                        fecha_control = a.FechaControl,
                        documentos = a.Documentos,
                        observaciones = a.Observaciones,
                        subdominio = new
                        {
                            id = a.SubdominioId,
                            practicas_gobierno = subdominio?.PracticasGobierno ?? "Sin subdominio",
                            indicadores_asociados = subdominio?.IndicadoresAsociados ?? "Sin indicadores",
                            proceso = new
                            {
                                id = subdominio?.ProcesoId ?? 0,
                                nombre = proceso?.Nombre ?? "Sin proceso",
                                codigo = proceso?.Codigo ?? "Sin código",
                                dominio = new
                                {
                                    id = proceso?.DominioId ?? 0,
                                    nombre = dominio?.Nombre ?? "Sin dominio"
                                }
                            }
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades con detalle completo");
                return new List<object>();
            }
        }

        public async Task<object?> ObtenerActividadConDetalleCompletoAsync(int id)
        {
            try
            {
                var actividad = await _actividadRepository.ObtenerPorId(id);
                if (actividad == null) return null;

                var subdominio = await _subdominioRepository.ObtenerPorId(actividad.SubdominioId);
                Proceso? proceso = null;
                Dominio? dominio = null;

                if (subdominio != null)
                {
                    proceso = await _procesoRepository.ObtenerPorId(subdominio.ProcesoId);
                    if (proceso != null)
                    {
                        dominio = await _dominioRepository.ObtenerPorId(proceso.DominioId);
                    }
                }

                return new
                {
                    id = actividad.IdActividad,
                    nombre = actividad.Nombre,
                    implementable = actividad.Implementable,
                    fecha_compromiso = actividad.FechaCompromiso,
                    estado_implementacion = actividad.EstadoImplementacion,
                    porcentaje_avance = actividad.PorcentajeAvance,
                    funcionarios_responsables_id = actividad.FuncionariosResponsablesId,
                    funcionarios_responsables_nombre = actividad.FuncionariosResponsables?.nombre ?? "Sin responsable",
                    funcionarios_responsables = new
                    {
                        id = actividad.FuncionariosResponsablesId,
                        nombre = actividad.FuncionariosResponsables?.nombre ?? "Sin responsable",
                        cedula = actividad.FuncionariosResponsables?.cedula ?? string.Empty,
                        correo_electronico = actividad.FuncionariosResponsables?.correo_electronico ?? string.Empty
                    },
                    fecha_control = actividad.FechaControl,
                    documentos = actividad.Documentos,
                    observaciones = actividad.Observaciones,
                    subdominio = new
                    {
                        id = actividad.SubdominioId,
                        practicas_gobierno = subdominio?.PracticasGobierno ?? "Sin subdominio",
                        indicadores_asociados = subdominio?.IndicadoresAsociados ?? "Sin indicadores",
                        proceso = new
                        {
                            id = subdominio?.ProcesoId ?? 0,
                            nombre = proceso?.Nombre ?? "Sin proceso",
                            codigo = proceso?.Codigo ?? "Sin código",
                            dominio = new
                            {
                                id = proceso?.DominioId ?? 0,
                                nombre = dominio?.Nombre ?? "Sin dominio"
                            }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividad con detalle completo: {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<object>> ObtenerActividadesPorResponsableAsync(int funcionariosResponsablesId)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesResponsable = actividades.Where(a => a.FuncionariosResponsablesId == funcionariosResponsablesId);

                var subdominios = await _subdominioRepository.ObtenerTodos();

                return actividadesResponsable.Select(a => new
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
                    observaciones = a.Observaciones,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades por responsable: {ResponsableId}", funcionariosResponsablesId);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> FiltrarActividadesPorEstadoAsync(string estado)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesFiltradas = actividades.Where(a =>
                    a.EstadoImplementacion.Equals(estado, StringComparison.OrdinalIgnoreCase));

                var subdominios = await _subdominioRepository.ObtenerTodos();

                return actividadesFiltradas.Select(a => new
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
                    observaciones = a.Observaciones,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al filtrar actividades por estado: {Estado}", estado);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> FiltrarActividadesPorImplementableAsync(string implementable)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesFiltradas = actividades.Where(a =>
                    a.Implementable.Equals(implementable, StringComparison.OrdinalIgnoreCase));

                var subdominios = await _subdominioRepository.ObtenerTodos();

                return actividadesFiltradas.Select(a => new
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
                    observaciones = a.Observaciones,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al filtrar actividades por implementable: {Implementable}", implementable);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerActividadesPorRangoAvanceAsync(decimal minPorcentaje, decimal maxPorcentaje)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesFiltradas = actividades.Where(a =>
                    a.PorcentajeAvance >= minPorcentaje && a.PorcentajeAvance <= maxPorcentaje);

                var subdominios = await _subdominioRepository.ObtenerTodos();

                return actividadesFiltradas.Select(a => new
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
                    observaciones = a.Observaciones,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades por rango de avance: {Min} - {Max}", minPorcentaje, maxPorcentaje);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerActividadesPorFechaCompromisoAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesFiltradas = actividades.Where(a =>
                    a.FechaCompromiso.HasValue &&
                    a.FechaCompromiso.Value.Date >= fechaInicio.Date &&
                    a.FechaCompromiso.Value.Date <= fechaFin.Date);

                var subdominios = await _subdominioRepository.ObtenerTodos();

                return actividadesFiltradas.Select(a => new
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
                    observaciones = a.Observaciones,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades por fecha compromiso: {FechaInicio} - {FechaFin}", fechaInicio, fechaFin);
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerActividadesVencidasAsync()
        {
            try
            {
                var fechaActual = DateTime.Now.Date;
                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesVencidas = actividades.Where(a =>
                    a.FechaCompromiso.HasValue &&
                    a.FechaCompromiso.Value.Date < fechaActual &&
                    !a.EstadoImplementacion.Equals("Implementado", StringComparison.OrdinalIgnoreCase));

                var subdominios = await _subdominioRepository.ObtenerTodos();

                return actividadesVencidas.Select(a => new
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
                    observaciones = a.Observaciones,
                    dias_vencida = a.FechaCompromiso.HasValue ? (fechaActual - a.FechaCompromiso.Value.Date).Days : 0,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades vencidas");
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> ObtenerActividadesProximasAVencerAsync(int diasAntelacion)
        {
            try
            {
                var fechaActual = DateTime.Now.Date;
                var fechaLimite = fechaActual.AddDays(diasAntelacion);

                var actividades = await _actividadRepository.ObtenerTodos();
                var actividadesProximas = actividades.Where(a =>
                    a.FechaCompromiso.HasValue &&
                    a.FechaCompromiso.Value.Date >= fechaActual &&
                    a.FechaCompromiso.Value.Date <= fechaLimite &&
                    !a.EstadoImplementacion.Equals("Implementado", StringComparison.OrdinalIgnoreCase));

                var subdominios = await _subdominioRepository.ObtenerTodos();

                return actividadesProximas.Select(a => new
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
                    observaciones = a.Observaciones,
                    dias_restantes = a.FechaCompromiso.HasValue ? (a.FechaCompromiso.Value.Date - fechaActual).Days : 0,
                    subdominio = new
                    {
                        id = a.SubdominioId,
                        practicas_gobierno = subdominios.FirstOrDefault(s => s.IdSubdominio == a.SubdominioId)?.PracticasGobierno ?? "Sin subdominio"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades próximas a vencer: {DiasAntelacion}", diasAntelacion);
                return new List<object>();
            }
        }

        public async Task<object> ObtenerEstadisticasActividadesAsync()
        {
            try
            {
                var actividades = await _actividadRepository.ObtenerTodos();
                var total = actividades.Count();

                var estadisticas = new
                {
                    total_actividades = total,
                    por_estado = actividades.GroupBy(a => a.EstadoImplementacion ?? "Sin Estado")
                        .Select(g => new { estado = g.Key, cantidad = g.Count() })
                        .OrderBy(x => x.estado),
                    por_implementable = actividades.GroupBy(a => a.Implementable ?? "Sin Definir")
                        .Select(g => new { implementable = g.Key, cantidad = g.Count() })
                        .OrderBy(x => x.implementable),
                    promedio_avance = total > 0 ? Math.Round(actividades.Average(a => a.PorcentajeAvance), 2) : 0,
                    avance_por_rangos = new
                    {
                        sin_iniciar = actividades.Count(a => a.PorcentajeAvance == 0),
                        en_progreso = actividades.Count(a => a.PorcentajeAvance > 0 && a.PorcentajeAvance < 100),
                        completado = actividades.Count(a => a.PorcentajeAvance == 100)
                    },
                    con_fecha_compromiso = actividades.Count(a => a.FechaCompromiso.HasValue),
                    vencidas = actividades.Count(a => a.FechaCompromiso.HasValue &&
                                                     a.FechaCompromiso.Value.Date < DateTime.Now.Date &&
                                                     a.PorcentajeAvance < 100)
                };

                return estadisticas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de actividades");
                return new { error = "Error al obtener estadísticas" };
            }
        }

        public async Task<object> ObtenerEstadisticasPorSubdominioAsync(int subdominioId)
        {
            try
            {
                var subdominio = await _subdominioRepository.ObtenerPorId(subdominioId);
                if (subdominio == null)
                    return new { error = "Subdominio no encontrado" };

                var actividades = await _actividadRepository.ObtenerPorIdSubdominio(subdominioId);
                var total = actividades.Count();

                var estadisticas = new
                {
                    subdominio_id = subdominioId,
                    subdominio_nombre = subdominio.PracticasGobierno,
                    total_actividades = total,
                    por_estado = actividades.GroupBy(a => a.EstadoImplementacion ?? "Sin Estado")
                        .Select(g => new { estado = g.Key, cantidad = g.Count() })
                        .OrderBy(x => x.estado),
                    promedio_avance = total > 0 ? Math.Round(actividades.Average(a => a.PorcentajeAvance), 2) : 0,
                    avance_por_rangos = new
                    {
                        sin_iniciar = actividades.Count(a => a.PorcentajeAvance == 0),
                        en_progreso = actividades.Count(a => a.PorcentajeAvance > 0 && a.PorcentajeAvance < 100),
                        completado = actividades.Count(a => a.PorcentajeAvance == 100)
                    }
                };

                return estadisticas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas del subdominio {SubdominioId}", subdominioId);
                return new { error = "Error al obtener estadísticas del subdominio" };
            }
        }

        public async Task<object> ObtenerMisActividadesAsync(int usuarioId)
        {

            try
            {
                var todasActividades = await _actividadRepository.ObtenerTodos();
                var misActividades = todasActividades
                    .Where(a => a.FuncionariosResponsablesId == usuarioId)
                    .ToList();

                var subdominiosIds = misActividades.Select(a => a.SubdominioId).Distinct().ToList();
                var subdominios = await _subdominioRepository.ObtenerTodos();
                var subdominiosDict = subdominios.ToDictionary(s => s.IdSubdominio);

                var procesos = await _procesoRepository.ObtenerTodos();
                var procesosDict = procesos.ToDictionary(p => p.IdProceso);

                var dominios = await _dominioRepository.ObtenerTodos();
                var dominiosDict = dominios.ToDictionary(d => d.IdDominio);

                var mapear = (Actividad a) =>
                {
                    var sub = subdominiosDict.ContainsKey(a.SubdominioId) ? subdominiosDict[a.SubdominioId] : null;
                    var proceso = sub != null && procesosDict.ContainsKey(sub.ProcesoId) ? procesosDict[sub.ProcesoId] : null;
                    var dominio = proceso != null && dominiosDict.ContainsKey(proceso.DominioId) ? dominiosDict[proceso.DominioId] : null;

                    return new
                    {
                        idActividad = a.IdActividad,
                        nombre = a.Nombre,
                        estadoImplementacion = a.EstadoImplementacion,
                        porcentajeAvance = a.PorcentajeAvance,
                        fechaCompromiso = a.FechaCompromiso,
                        subdominioId = a.SubdominioId,
                        subdominioNombre = sub?.PracticasGobierno ?? "Sin subdominio",
                        subdominio = sub == null ? null : new
                        {
                            id = sub.IdSubdominio,
                            practicasGobierno = sub.PracticasGobierno,
                            procesoId = sub.ProcesoId,
                            proceso = proceso == null ? null : new
                            {
                                id = proceso.IdProceso,
                                nombre = proceso.Nombre,
                                dominioId = proceso.DominioId,
                                dominio = dominio == null ? null : new
                                {
                                    id = dominio.IdDominio,
                                    nombre = dominio.Nombre
                                }
                            }
                        }
                    };
                };

                var pendientes = misActividades
                    .Where(a => a.EstadoImplementacion != "Implementado" && a.FechaCompromiso.HasValue && a.FechaCompromiso.Value.Date >= DateTime.Today.Date)
                    .Select(mapear)
                    .OrderBy(a => a.fechaCompromiso)
                    .ToList();

                var completadas = misActividades
                    .Where(a => a.EstadoImplementacion == "Implementado")
                    .Select(mapear)
                    .OrderByDescending(a => a.idActividad)
                    .ToList();

                var vencidas = misActividades
                    .Where(a => a.EstadoImplementacion != "Implementado" && a.FechaCompromiso.HasValue && a.FechaCompromiso.Value.Date < DateTime.Today.Date)
                    .Select(mapear)
                    .OrderByDescending(a => a.idActividad)
                    .ToList();

                return new
                {
                    pendientes,
                    completadas,
                    vencidas,
                    totalPendientes = pendientes.Count,
                    totalCompletadas = completadas.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividades del usuario {UsuarioId}", usuarioId);
                return new { error = "Error al obtener actividades" };
            }
        }

        public async Task<object> enviarCorreosVencimientoActividadesAsync()
        {
            try
            {
                var actividadesPendientes7Dias = await _actividadRepository.ObtenerActividadesPendientesPorDiasVencimiento(7);
                var actividadesPendientes0Dias = await _actividadRepository.ObtenerActividadesPendientesPorDiasVencimiento(0);
                var actividadesVencidas = await _actividadRepository.ObtenerActividadesPendientesPorDiasVencimiento(-1);

                var actividadesPendientes = actividadesPendientes7Dias.Concat(actividadesPendientes0Dias).ToList().Concat(actividadesVencidas).ToList();
                foreach (var actividad in actividadesPendientes)
                {
                    if (actividad.FuncionariosResponsables != null)
                    {
                        var usuario = actividad.FuncionariosResponsables;
                        if (usuario != null)
                        {
                            await _emailService.EnviarAlertaVencimientoActividad(
                                usuario.correo_electronico,
                                usuario.nombre,
                                actividad.FechaCompromiso.HasValue ? (actividad.FechaCompromiso.Value.Date - DateTime.Today.Date).Days : 0,
                                actividad.Nombre);
                        }
                    }
                }


                return new { mensaje = "Correos de alerta de vencimiento enviados exitosamente" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correos de alerta de vencimiento");
                return new { error = "Error al enviar correos de alerta de vencimiento" };
            }
        }
    }
}