using backend.Models;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Dtos;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using IDocumentoService = backend.Services.Interfaces.IDocumentoService;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/subdominios/{subdominioId:int}/actividades")]
    public class ActividadesController : ControllerBase
    {
        private readonly IActividadService _actividadService;
        private readonly ISubdominioService _subdominioService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IDocumentoService _documentoService;
        private readonly IHistorialActividadService _historialActividadService;

        public ActividadesController(
            IActividadService actividadService,
            ISubdominioService subdominioService,
            IAuditoriaService auditoriaService,
            IDocumentoService documentoService,
            IHistorialActividadService historialActividadService)
        {
            _actividadService = actividadService;
            _subdominioService = subdominioService;
            _auditoriaService = auditoriaService;
            _documentoService = documentoService;
            _historialActividadService = historialActividadService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> Listar(int subdominioId, CancellationToken ct)
        {
            try
            {
                var actividades = await _actividadService.ObtenerActividadesPorSubdominioAsync(subdominioId);
                return Ok(actividades);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // SOLO ADMIN o SUPERADMIN
        [HttpPost]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<ActionResult<object>> Crear(
            int subdominioId,
            [FromBody] CrearActividadRequest req,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest("Nombre es requerido");

            if (req.FuncionariosResponsablesId <= 0)
                return BadRequest("El responsable es requerido");

            var sid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var usuarioId = int.TryParse(sid, out var parsed) ? parsed : 1;

            try
            {
                var resultado = await _actividadService.CrearActividadAsync(
                    req.Nombre.Trim(),
                    "Sí",
                    req.FuncionariosResponsablesId,
                    subdominioId);

                if (resultado.StartsWith("Error:"))
                {
                    if (resultado.Contains("no encontrado"))
                        return NotFound(resultado);
                    else if (resultado.Contains("Ya existe"))
                        return Conflict(resultado);
                    else
                        return BadRequest(resultado);
                }

                // El resultado debería tener el formato "Actividad creada exitosamente: {ID}"
                if (!resultado.Contains(":"))
                {
                    return StatusCode(500, "Error: Formato de respuesta inesperado del servicio");
                }

                var parts = resultado.Split(':');
                if (parts.Length < 2 || !int.TryParse(parts[1].Trim(), out var actividadId))
                {
                    return StatusCode(500, "Error: No se pudo obtener el ID de la actividad creada");
                }

                var actividad = await _actividadService.ObtenerActividadPorIdAsync(actividadId);

                await _auditoriaService.RegistrarEventoAsync(
                    "Creación",
                    $"Actividad creada: '{req.Nombre.Trim()}' (ID {actividadId}) en subdominio {subdominioId}",
                    "Actividades",
                    usuarioId,
                    datosNuevos: actividad);

                return CreatedAtAction(nameof(Obtener),
                    new { subdominioId, id = actividadId },
                    actividad);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> Obtener(int subdominioId, int id, CancellationToken ct)
        {
            try
            {
                var actividad = await _actividadService.ObtenerActividadPorIdAsync(id);

                if (actividad == null)
                    return NotFound("Actividad no encontrada");

                return Ok(actividad);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // ADMIN, SUPERADMIN o el funcionario responsable de la actividad
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<object>> Actualizar(
    int subdominioId,
    int id,
    [FromBody] Backend.Dtos.ActualizarActividadRequest req,
    CancellationToken ct)
        {
            try
            {
                var actividadAntes = await _actividadService.ObtenerActividadPorIdAsync(id);

                if (actividadAntes == null)
                    return NotFound("Actividad no encontrada");

                var resultados = new List<string>();

                var usuarioIdClaim = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? usuarioId = int.TryParse(usuarioIdClaim, out var parsedUsuarioId) ? parsedUsuarioId : null;

                // Verificar permisos: admin, superadmin o responsable de la actividad
                // El JWT guarda el rol con la clave "rol" (no ClaimTypes.Role)
                var rol = HttpContext?.User?.FindFirst("rol")?.Value
                       ?? HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value
                       ?? "";
                var esAdmin = rol == "ADMIN" || rol == "SUPERADMIN";

                var responsableActualProp = actividadAntes.GetType()
                    .GetProperty("FuncionariosResponsablesId",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.IgnoreCase);

                var responsableActualObj = responsableActualProp?.GetValue(actividadAntes);

                int responsableActual = responsableActualObj != null
                    ? Convert.ToInt32(responsableActualObj)
                    : 0;

                var esResponsable = usuarioId.HasValue && usuarioId.Value == responsableActual;

                if (!esAdmin && !esResponsable)
                    return Forbid();

                var implementableActualProp = actividadAntes.GetType()
                    .GetProperty("Implementable",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.IgnoreCase);

                var implementableActualObj = implementableActualProp?.GetValue(actividadAntes);
                var implementableActual = (implementableActualObj?.ToString() ?? "Sí").Trim();
                var implementableObjetivo = string.IsNullOrWhiteSpace(req.Implementable)
                    ? implementableActual
                    : req.Implementable.Trim();

                // Solo ADMIN y SUPERADMIN pueden modificar Implementable
                if (!string.IsNullOrWhiteSpace(req.Implementable) && !esAdmin)
                {
                    return StatusCode(403, "Solo administradores y superadministradores pueden modificar el campo Implementable.");
                }

                var quedaraNoImplementable = string.Equals(implementableObjetivo, "No", StringComparison.OrdinalIgnoreCase);
                var intentaEditarCamposBloqueados =
                    req.FechaCompromiso.HasValue ||
                    !string.IsNullOrWhiteSpace(req.EstadoImplementacion) ||
                    req.PorcentajeAvance.HasValue ||
                    req.FuncionariosResponsablesId.HasValue ||
                    req.FechaControl.HasValue ||
                    req.Documentos != null;

                if (quedaraNoImplementable && intentaEditarCamposBloqueados)
                {
                    return BadRequest("La actividad esta marcada como no implementable. Los campos de implementacion y documentos relacionados estan bloqueados.");
                }

                var hayCambiosSolicitados =
                    !string.IsNullOrWhiteSpace(req.Nombre) ||
                    !string.IsNullOrWhiteSpace(req.Implementable) ||
                    req.FuncionariosResponsablesId.HasValue ||
                    !string.IsNullOrWhiteSpace(req.EstadoImplementacion) ||
                    req.PorcentajeAvance.HasValue ||
                    req.FechaCompromiso.HasValue ||
                    req.FechaControl.HasValue ||
                    req.Documentos != null ||
                    req.Observaciones != null;

                var descripcionCambios = ConstruirDescripcionCambios(req);

                if (!string.IsNullOrWhiteSpace(req.Nombre) ||
                    !string.IsNullOrWhiteSpace(req.Implementable) ||
                    req.FuncionariosResponsablesId.HasValue)
                {
                    // Si se marca como "No Implementable", limpiar el responsable
                    int? responsableFinal = null;
                    if (string.Equals(implementableObjetivo, "No", StringComparison.OrdinalIgnoreCase))
                    {
                        responsableFinal = null;
                    }
                    else if (req.FuncionariosResponsablesId.HasValue)
                    {
                        responsableFinal = req.FuncionariosResponsablesId.Value;
                    }
                    else
                    {
                        responsableFinal = responsableActual;
                    }

                    var resultado = await _actividadService.ActualizarActividadAsync(
                        id,
                        req.Nombre?.Trim() ?? "",
                        implementableObjetivo,
                        responsableFinal,
                        subdominioId);

                    if (resultado.StartsWith("Error:"))
                    {
                        if (resultado.Contains("no encontrada"))
                            return NotFound(resultado);
                        else if (resultado.Contains("Ya existe"))
                            return Conflict(resultado);
                        else
                            return BadRequest(resultado);
                    }

                    resultados.Add(resultado);
                }

                if (!string.IsNullOrWhiteSpace(req.EstadoImplementacion))
                {
                    var resultado = await _actividadService.ActualizarEstadoImplementacionAsync(id, req.EstadoImplementacion);
                    if (resultado.StartsWith("Error:"))
                        return BadRequest(resultado);
                    resultados.Add(resultado);
                }

                if (req.PorcentajeAvance.HasValue)
                {
                    var porcentaje = Math.Min(100m, Math.Max(0m, req.PorcentajeAvance.Value));
                    var resultado = await _actividadService.ActualizarPorcentajeAvanceAsync(id, porcentaje);
                    if (resultado.StartsWith("Error:"))
                        return BadRequest(resultado);
                    resultados.Add(resultado);
                }

                if (req.FechaCompromiso.HasValue)
                {
                    var resultado = await _actividadService.ActualizarFechaCompromisoAsync(id, req.FechaCompromiso);
                    if (resultado.StartsWith("Error:"))
                        return BadRequest(resultado);
                    resultados.Add(resultado);
                }

                if (req.FechaControl.HasValue)
                {
                    var resultado = await _actividadService.ActualizarFechaControlAsync(id, req.FechaControl);
                    if (resultado.StartsWith("Error:"))
                        return BadRequest(resultado);
                    resultados.Add(resultado);
                }

                if (req.Documentos != null)
                {
                    var resultado = await _actividadService.ActualizarDocumentosAsync(id, req.Documentos);
                    if (resultado.StartsWith("Error:"))
                        return BadRequest(resultado);
                    resultados.Add(resultado);
                }

                if (req.Observaciones != null)
                {
                    var resultado = await _actividadService.ActualizarObservacionesAsync(id, req.Observaciones);
                    if (resultado.StartsWith("Error:"))
                        return BadRequest(resultado);
                    resultados.Add(resultado);
                }

                if (hayCambiosSolicitados)
                {
                    await _historialActividadService.RegistrarVersionAnteriorAsync(id, descripcionCambios, usuarioId);
                }

                var actividadActualizada = await _actividadService.ObtenerActividadPorIdAsync(id);

                await _auditoriaService.RegistrarEventoAsync(
                    "Modificación",
                    $"Actividad {id} actualizada",
                    "Actividades",
                    usuarioId,
                    datosAnteriores: actividadAntes,
                    datosNuevos: actividadActualizada);

                return Ok(actividadActualizada);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("{id:int}/historial")]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerHistorialVersiones(
            int subdominioId,
            int id,
            CancellationToken ct)
        {
            try
            {
                var historial = await _historialActividadService.ObtenerHistorialPorActividadAsync(id);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET /api/subdominios/{subdominioId}/actividades/mis-actividades
        // Devuelve actividades del usuario autenticado (pendientes y completadas)
        [HttpGet("~/api/actividades/mis-actividades")]
        public async Task<ActionResult<object>> MisActividades(CancellationToken ct)
        {
            var sid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(sid, out var usuarioId))
                return Unauthorized();

            var resultado = await _actividadService.ObtenerMisActividadesAsync(usuarioId);
            return Ok(resultado);
        }

        // GET /api/actividades/en-revision
        // Lista actividades pendientes de aceptación administrativa.
        [HttpGet("~/api/actividades/en-revision")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerActividadesEnRevision(CancellationToken ct)
        {
            try
            {
                var actividades = await _actividadService.ObtenerTodasLasActividadesAsync();

                var enRevision = actividades
                    .Where(a =>
                    {
                        var estado = a.GetType().GetProperty("estado_implementacion")?.GetValue(a)?.ToString();
                        if (string.IsNullOrWhiteSpace(estado)) return false;

                        var normalizado = estado.Trim().ToLowerInvariant().Replace("_", " ");
                        return normalizado == "en revisión" || normalizado == "en revision";
                    })
                    .OrderBy(a =>
                    {
                        var fecha = a.GetType().GetProperty("fecha_compromiso")?.GetValue(a) as DateTime?;
                        return fecha ?? DateTime.MaxValue;
                    })
                    .ToList();

                return Ok(enRevision);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET /api/actividades/reporte-revision-por-usuario
        // Reporte de actividades en revisión agrupadas por usuario responsable
        [HttpGet("~/api/actividades/reporte-revision-por-usuario")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<ActionResult<object>> ObtenerReporteActividadesEnRevisionPorUsuario(CancellationToken ct)
        {
            try
            {
                var actividades = await _actividadService.ObtenerTodasLasActividadesAsync();

                var enRevision = actividades
                    .Where(a =>
                    {
                        var estado = a.GetType().GetProperty("estado_implementacion")?.GetValue(a)?.ToString();
                        if (string.IsNullOrWhiteSpace(estado)) return false;

                        var normalizado = estado.Trim().ToLowerInvariant().Replace("_", " ");
                        return normalizado == "en revisión" || normalizado == "en revision";
                    })
                    .ToList();

                var actividadesPorUsuario = enRevision
                    .GroupBy(a =>
                    {
                        var responsable = a.GetType().GetProperty("funcionarios_responsables")?.GetValue(a);
                        return new
                        {
                            usuarioId = responsable?.GetType().GetProperty("id")?.GetValue(responsable) ?? 0,
                            nombreCompleto = responsable?.GetType().GetProperty("nombre")?.GetValue(responsable)?.ToString() ?? "Sin responsable",
                            cedula = responsable?.GetType().GetProperty("cedula")?.GetValue(responsable)?.ToString() ?? "",
                            correoElectronico = responsable?.GetType().GetProperty("correo_electronico")?.GetValue(responsable)?.ToString() ?? ""
                        };
                    })
                    .Select(g => new
                    {
                        usuario = new
                        {
                            id = g.Key.usuarioId,
                            nombreCompleto = g.Key.nombreCompleto,
                            cedula = g.Key.cedula,
                            correoElectronico = g.Key.correoElectronico
                        },
                        totalActividadesEnRevision = g.Count(),
                        actividades = g
                            .OrderBy(a =>
                            {
                                var fecha = a.GetType().GetProperty("fecha_compromiso")?.GetValue(a) as DateTime?;
                                return fecha ?? DateTime.MaxValue;
                            })
                            .Select(a => new
                            {
                                idActividad = a.GetType().GetProperty("id_actividad")?.GetValue(a) ?? a.GetType().GetProperty("id")?.GetValue(a),
                                nombre = a.GetType().GetProperty("nombre")?.GetValue(a)?.ToString() ?? "Sin nombre",
                                subdominioNombre = a.GetType().GetProperty("subdominio")?.GetValue(a)
                                    ?.GetType().GetProperty("practicas_gobierno")?.GetValue(a.GetType().GetProperty("subdominio")?.GetValue(a))?.ToString() ?? "Sin subdominio",
                                porcentajeAvance = a.GetType().GetProperty("porcentaje_avance")?.GetValue(a),
                                fechaCompromiso = a.GetType().GetProperty("fecha_compromiso")?.GetValue(a),
                                observaciones = a.GetType().GetProperty("observaciones")?.GetValue(a)?.ToString() ?? ""
                            })
                            .ToList()
                    })
                    .OrderByDescending(x => x.totalActividadesEnRevision)
                    .ThenBy(x => x.usuario.nombreCompleto)
                    .ToList();

                return Ok(new
                {
                    generadoEn = DateTime.UtcNow,
                    totalUsuarios = actividadesPorUsuario.Count,
                    totalActividadesEnRevision = enRevision.Count,
                    usuariosConActividades = actividadesPorUsuario
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET /api/actividades/reporte-seguimiento
        // Reporte manual de usuarios con tareas pendientes para seguimiento administrativo.
        [HttpGet("~/api/actividades/reporte-seguimiento")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<ActionResult<object>> ObtenerReporteSeguimientoPendientes(
            [FromQuery] bool incluirPorcentajeSinActualizar = true,
            [FromQuery] bool incluirEstadoIncompleto = true,
            [FromQuery] bool incluirFechaControlSinAsignar = true,
            CancellationToken ct = default)
        {
            if (!incluirPorcentajeSinActualizar && !incluirEstadoIncompleto && !incluirFechaControlSinAsignar)
            {
                return BadRequest("Debe seleccionar al menos un tipo de tarea para el reporte.");
            }

            try
            {
                var actividades = await _actividadService.ObtenerActividadesConDetalleCompletoAsync();
                var hoy = DateTime.Today;
                var limiteProxima = hoy.AddDays(7);

                var filas = new List<ReportePendienteFila>();

                foreach (var actividad in actividades)
                {
                    var responsable = ObtenerPropiedad(actividad, "funcionarios_responsables");

                    var usuarioId = ObtenerEntero(responsable, "id")
                                   ?? ObtenerEntero(actividad, "funcionarios_responsables_id")
                                   ?? 0;

                    var nombreResponsable = ObtenerTexto(responsable, "nombre")
                                            ?? ObtenerTexto(actividad, "funcionarios_responsables_nombre")
                                            ?? "Sin responsable";

                    var cedula = ObtenerTexto(responsable, "cedula") ?? string.Empty;
                    var correo = ObtenerTexto(responsable, "correo_electronico") ?? string.Empty;

                    var estado = ObtenerTexto(actividad, "estado_implementacion") ?? string.Empty;
                    var porcentaje = ObtenerDecimal(actividad, "porcentaje_avance") ?? 0m;
                    var fechaControl = ObtenerFecha(actividad, "fecha_control");
                    var fechaCompromiso = ObtenerFecha(actividad, "fecha_compromiso");

                    var tiposPendientes = new List<string>();

                    if (incluirPorcentajeSinActualizar && porcentaje <= 0m)
                        tiposPendientes.Add("Porcentaje de avance sin actualizar");

                    if (incluirEstadoIncompleto && !EsEstadoImplementado(estado))
                        tiposPendientes.Add("Estado de implementacion incompleta");

                    if (incluirFechaControlSinAsignar && !fechaControl.HasValue)
                        tiposPendientes.Add("Fecha de control sin asignar");

                    if (tiposPendientes.Count == 0)
                        continue;

                    var subdominio = ObtenerPropiedad(actividad, "subdominio");
                    var nombreSubdominio = ObtenerTexto(subdominio, "practicas_gobierno") ?? "Sin subdominio";

                    var proceso = ObtenerPropiedad(subdominio, "proceso");
                    var procesoCodigo = ObtenerTexto(proceso, "codigo");
                    var procesoNombre = ObtenerTexto(proceso, "nombre") ?? "Sin proceso";
                    var procesoRelacionado = string.IsNullOrWhiteSpace(procesoCodigo)
                        ? procesoNombre
                        : $"{procesoCodigo} - {procesoNombre}";

                    filas.Add(new ReportePendienteFila
                    {
                        UsuarioId = usuarioId,
                        NombreCompleto = nombreResponsable,
                        Cedula = cedula,
                        CorreoElectronico = correo,
                        Actividad = ObtenerTexto(actividad, "nombre") ?? "Sin actividad",
                        ProcesoRelacionado = procesoRelacionado,
                        Subdominio = nombreSubdominio,
                        FechaCompromiso = fechaCompromiso,
                        FechaVencidaOProxima = ConstruirEtiquetaFecha(fechaCompromiso, hoy, limiteProxima),
                        TiposAccionPendiente = tiposPendientes
                    });
                }

                var agrupadoPorUsuario = filas
                    .GroupBy(f => new
                    {
                        f.UsuarioId,
                        f.NombreCompleto,
                        f.Cedula,
                        f.CorreoElectronico
                    })
                    .Select(g => new
                    {
                        usuario = new
                        {
                            id = g.Key.UsuarioId,
                            nombreCompleto = g.Key.NombreCompleto,
                            cedula = g.Key.Cedula,
                            correoElectronico = g.Key.CorreoElectronico
                        },
                        totalTareasPendientes = g.Count(),
                        tareas = g
                            .OrderBy(t => t.FechaCompromiso ?? DateTime.MaxValue)
                            .ThenBy(t => t.Actividad)
                            .Select(t => new
                            {
                                tipoAccionPendiente = t.TiposAccionPendiente,
                                actividad = t.Actividad,
                                procesoRelacionado = t.ProcesoRelacionado,
                                subdominio = t.Subdominio,
                                fechaCompromiso = t.FechaCompromiso,
                                fechaVencidaOProxima = t.FechaVencidaOProxima
                            })
                            .ToList()
                    })
                    .OrderByDescending(x => x.totalTareasPendientes)
                    .ThenBy(x => x.usuario.nombreCompleto)
                    .ToList();

                return Ok(new
                {
                    generadoEn = DateTime.UtcNow,
                    filtrosAplicados = new
                    {
                        porcentajeSinActualizar = incluirPorcentajeSinActualizar,
                        estadoImplementacionIncompleta = incluirEstadoIncompleto,
                        fechaControlSinAsignar = incluirFechaControlSinAsignar
                    },
                    totalUsuarios = agrupadoPorUsuario.Count,
                    totalTareasPendientes = filas.Count,
                    usuarios = agrupadoPorUsuario
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // Endpoint para obtener estadísticas de actividades del subdominio
        [HttpGet("estadisticas")]
        public async Task<ActionResult<object>> ObtenerEstadisticas(int subdominioId, CancellationToken ct)
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

        // GET /api/subdominios/{subdominioId}/actividades/{id}/documentos
        [HttpGet("{id:int}/documentos")]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerDocumentos(
            int subdominioId, int id, CancellationToken ct)
        {
            try
            {
                var documentos = await _documentoService.ObtenerDocumentosActividadAsync(id);
                return Ok(documentos);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPost("/api/actividades/enviar-alertas")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> EnviarCorreosAlertasVencimientoActividades()
        {
            try
            {
                var resultado = await _actividadService.enviarCorreosVencimientoActividadesAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
        // GET /api/actividades-por-dominio
        [HttpGet("~/api/actividades-por-dominio")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerActividadesPorDominio([FromServices] IProcesoService procesoService, [FromServices] IDominioService dominioService, CancellationToken ct)
        {
            try
            {
                var actividades = await _actividadService.ObtenerTodasLasActividadesAsync();
                var subdominios = await _subdominioService.ObtenerTodosLosSubdominiosAsync();
                var procesos = await procesoService.ObtenerTodosLosProcesosAsync();
                var dominios = await dominioService.ObtenerTodosLosDominiosAsync();

                var actividadesPorDominio = dominios.Select(dominio =>
                {
                    var dominioId = dominio.GetType().GetProperty("id")?.GetValue(dominio) ?? dominio.GetType().GetProperty("IdDominio")?.GetValue(dominio);
                    var actividadesDominio = actividades
                        .Where(a =>
                        {
                            var subdominioObj = a.GetType().GetProperty("subdominio")?.GetValue(a);
                            var subdominioId = subdominioObj?.GetType().GetProperty("id")?.GetValue(subdominioObj);
                            var subdominio = subdominios.FirstOrDefault(s =>
                                (s.GetType().GetProperty("id")?.GetValue(s) ?? s.GetType().GetProperty("IdSubdominio")?.GetValue(s))?.Equals(subdominioId) == true
                            );
                            if (subdominio == null) return false;
                            var procesoObj = subdominio.GetType().GetProperty("proceso")?.GetValue(subdominio);
                            var procesoId = procesoObj?.GetType().GetProperty("id")?.GetValue(procesoObj);
                            var proceso = procesos.FirstOrDefault(p =>
                                (p.GetType().GetProperty("id")?.GetValue(p) ?? p.GetType().GetProperty("IdProceso")?.GetValue(p))?.Equals(procesoId) == true
                            );
                            if (proceso == null) return false;
                            var procesoDominioId = procesoObj?.GetType().GetProperty("dominioId")?.GetValue(procesoObj);
                            return procesoDominioId != null && dominioId != null && procesoDominioId.Equals(dominioId);
                        })
                        .ToList();

                    // Separar en pendientes, vencidas y completadas
                    var pendientes = actividadesDominio
                        .Where(a =>
                        {
                            var estado = a.GetType().GetProperty("estado_implementacion")?.GetValue(a)?.ToString();
                            var fecha = a.GetType().GetProperty("fecha_compromiso")?.GetValue(a) as DateTime?;
                            return estado != "Implementado" && fecha.HasValue && fecha.Value.Date >= DateTime.Today.Date;
                        })
                        .ToList();
                    var vencidas = actividadesDominio
                        .Where(a =>
                        {
                            var estado = a.GetType().GetProperty("estado_implementacion")?.GetValue(a)?.ToString();
                            var fecha = a.GetType().GetProperty("fecha_compromiso")?.GetValue(a) as DateTime?;
                            return estado != "Implementado" && fecha.HasValue && fecha.Value.Date < DateTime.Today.Date;
                        })
                        .ToList();
                    var completadas = actividadesDominio
                        .Where(a =>
                        {
                            var estado = a.GetType().GetProperty("estado_implementacion")?.GetValue(a)?.ToString();
                            return estado == "Implementado";
                        })
                        .ToList();

                    return new
                    {
                        dominio = new
                        {
                            id = dominioId,
                            nombre = dominio.GetType().GetProperty("nombre")?.GetValue(dominio) ?? dominio.GetType().GetProperty("Nombre")?.GetValue(dominio),
                            cantidad_procesos = dominio.GetType().GetProperty("cantidad_procesos")?.GetValue(dominio) ?? (dominio.GetType().GetProperty("Procesos")?.GetValue(dominio) as ICollection<object>)?.Count ?? 0
                        },
                        pendientes,
                        vencidas,
                        completadas
                    };
                }).ToList();

                return Ok(actividadesPorDominio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
        private static string ConstruirDescripcionCambios(Backend.Dtos.ActualizarActividadRequest req)
        {
            var cambios = new List<string>();

            if (!string.IsNullOrWhiteSpace(req.Nombre)) cambios.Add("Nombre");
            if (!string.IsNullOrWhiteSpace(req.Implementable)) cambios.Add("Implementable");
            if (req.FuncionariosResponsablesId.HasValue) cambios.Add("Funcionario responsable");
            if (!string.IsNullOrWhiteSpace(req.EstadoImplementacion)) cambios.Add("Estado de implementación");
            if (req.PorcentajeAvance.HasValue) cambios.Add("Porcentaje de avance");
            if (req.FechaCompromiso.HasValue) cambios.Add("Fecha compromiso");
            if (req.FechaControl.HasValue) cambios.Add("Fecha control");
            if (req.Documentos != null) cambios.Add("Documentos");
            if (req.Observaciones != null) cambios.Add("Descripción/observaciones");

            return cambios.Count == 0
                ? "Actualización general de actividad"
                : $"Campos modificados: {string.Join(", ", cambios)}";
        }

        private static object? ObtenerPropiedad(object? origen, string nombrePropiedad)
        {
            if (origen == null) return null;

            var prop = origen.GetType().GetProperty(
                nombrePropiedad,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            return prop?.GetValue(origen);
        }

        private static string? ObtenerTexto(object? origen, string nombrePropiedad)
        {
            var valor = ObtenerPropiedad(origen, nombrePropiedad);
            if (valor == null) return null;

            var texto = valor.ToString();
            return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        }

        private static int? ObtenerEntero(object? origen, string nombrePropiedad)
        {
            var valor = ObtenerPropiedad(origen, nombrePropiedad);
            if (valor == null) return null;

            return int.TryParse(valor.ToString(), out var numero) ? numero : null;
        }

        private static decimal? ObtenerDecimal(object? origen, string nombrePropiedad)
        {
            var valor = ObtenerPropiedad(origen, nombrePropiedad);
            if (valor == null) return null;

            if (valor is decimal d) return d;
            if (valor is int i) return i;
            if (valor is double db) return (decimal)db;

            return decimal.TryParse(valor.ToString(), out var numero) ? numero : null;
        }

        private static DateTime? ObtenerFecha(object? origen, string nombrePropiedad)
        {
            var valor = ObtenerPropiedad(origen, nombrePropiedad);
            if (valor == null) return null;

            if (valor is DateTime dt) return dt;
            return DateTime.TryParse(valor.ToString(), out var parsed) ? parsed : null;
        }

        private static bool EsEstadoImplementado(string? estado)
        {
            var normalizado = (estado ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace("_", " ");

            return normalizado == "implementado";
        }

        private static string ConstruirEtiquetaFecha(DateTime? fechaCompromiso, DateTime hoy, DateTime limiteProxima)
        {
            if (!fechaCompromiso.HasValue)
                return "Sin fecha compromiso";

            var fecha = fechaCompromiso.Value.Date;
            var fechaTexto = fecha.ToString("dd/MM/yyyy");

            if (fecha < hoy)
                return $"Vencida ({fechaTexto})";

            if (fecha <= limiteProxima)
                return $"Proxima ({fechaTexto})";

            return $"Programada ({fechaTexto})";
        }

        private sealed class ReportePendienteFila
        {
            public int UsuarioId { get; set; }
            public string NombreCompleto { get; set; } = string.Empty;
            public string Cedula { get; set; } = string.Empty;
            public string CorreoElectronico { get; set; } = string.Empty;
            public List<string> TiposAccionPendiente { get; set; } = new();
            public string Actividad { get; set; } = string.Empty;
            public string ProcesoRelacionado { get; set; } = string.Empty;
            public string Subdominio { get; set; } = string.Empty;
            public DateTime? FechaCompromiso { get; set; }
            public string FechaVencidaOProxima { get; set; } = string.Empty;
        }
    }
}