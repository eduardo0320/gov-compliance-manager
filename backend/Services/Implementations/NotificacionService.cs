using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class NotificacionService : INotificacionService
    {
        private readonly INotificacionRepository _notificacionRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IActividadRepository _actividadRepository;
        private readonly ILogger<NotificacionService> _logger;

        public NotificacionService(
            INotificacionRepository notificacionRepository,
            IUsuarioRepository usuarioRepository,
            IActividadRepository actividadRepository,
            ILogger<NotificacionService> logger)
        {
            _notificacionRepository = notificacionRepository;
            _usuarioRepository = usuarioRepository;
            _actividadRepository = actividadRepository;
            _logger = logger;
        }

        public async Task CrearNotificacionAsync(int usuarioDestinoId, string titulo, string mensaje, string tipo = "info", string? urlDestino = null)
        {
            try
            {
                var notificacion = new Notificacion
                {
                    UsuarioDestinoId = usuarioDestinoId,
                    Titulo = titulo,
                    Mensaje = mensaje,
                    Tipo = tipo,
                    UrlDestino = urlDestino,
                    Leida = false,
                    FechaCreacion = DateTime.UtcNow
                };

                await _notificacionRepository.Agregar(notificacion);
                await _notificacionRepository.GuardarCambios();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando notificación para usuario {UsuarioId}", usuarioDestinoId);
            }
        }

        public async Task GenerarNotificacionesVencimientoActividadesUsuarioAsync(int usuarioId)
        {
            try
            {
                var hoy = DateTime.Today;
                var inicioDiaUtc = DateTime.UtcNow.Date;
                var finDiaUtc = inicioDiaUtc.AddDays(1);

                var actividadesRaw = await _actividadRepository.BuscarAsync(a =>
                        a.FuncionariosResponsablesId == usuarioId &&
                        a.EstadoImplementacion != "Implementado" &&
                        a.FechaCompromiso.HasValue);

                var actividades = actividadesRaw.Select(a => new
                {
                    a.IdActividad,
                    a.SubdominioId,
                    a.Nombre,
                    FechaCompromiso = a.FechaCompromiso!.Value.Date
                }).ToList();

                if (!actividades.Any())
                    return;

                var titulosVencimiento = new[]
                {
                    "Actividad vencida",
                    "Actividad vence hoy",
                    "Actividad próxima a vencer"
                };

                var notificacionesHoy = (await _notificacionRepository.BuscarAsync(n =>
                        n.UsuarioDestinoId == usuarioId &&
                        titulosVencimiento.Contains(n.Titulo) &&
                        n.FechaCreacion >= inicioDiaUtc &&
                        n.FechaCreacion < finDiaUtc)).ToList();

                var existentesPorClave = notificacionesHoy
                    .GroupBy(n => $"{n.Tipo}|{n.Titulo}|{n.Mensaje}")
                    .ToDictionary(g => g.Key, g => g.First());

                var clavesExistentes = new HashSet<string>(existentesPorClave.Keys);

                var nuevas = new List<Notificacion>();
                var hayCambiosEnExistentes = false;

                foreach (var actividad in actividades)
                {
                    var diasRestantes = (actividad.FechaCompromiso - hoy).Days;

                    string? tipo = null;
                    string? titulo = null;
                    string? mensaje = null;

                    if (diasRestantes < 0)
                    {
                        tipo = "danger";
                        titulo = "Actividad vencida";
                        mensaje = $"La actividad \"{actividad.Nombre}\" ya venció (fecha compromiso: {actividad.FechaCompromiso:dd/MM/yyyy}).";
                    }
                    else if (diasRestantes == 0)
                    {
                        tipo = "warning";
                        titulo = "Actividad vence hoy";
                        mensaje = $"La actividad \"{actividad.Nombre}\" vence hoy ({actividad.FechaCompromiso:dd/MM/yyyy}).";
                    }
                    else if (diasRestantes == 7)
                    {
                        tipo = "info";
                        titulo = "Actividad próxima a vencer";
                        mensaje = $"La actividad \"{actividad.Nombre}\" vence en 7 días ({actividad.FechaCompromiso:dd/MM/yyyy}).";
                    }

                    if (tipo == null || titulo == null || mensaje == null)
                        continue;

                    var urlDestino = $"/subdominios/{actividad.SubdominioId}/actividades/{actividad.IdActividad}/editar";
                    var clave = $"{tipo}|{titulo}|{mensaje}";
                    if (clavesExistentes.Contains(clave))
                    {
                        if (existentesPorClave.TryGetValue(clave, out var notifExistente))
                        {
                            if (string.IsNullOrWhiteSpace(notifExistente.UrlDestino) || notifExistente.UrlDestino == "/misActividades")
                            {
                                notifExistente.UrlDestino = urlDestino;
                                hayCambiosEnExistentes = true;
                            }
                        }

                        continue;
                    }

                    nuevas.Add(new Notificacion
                    {
                        UsuarioDestinoId = usuarioId,
                        Titulo = titulo,
                        Mensaje = mensaje,
                        Tipo = tipo,
                        UrlDestino = urlDestino,
                        Leida = false,
                        FechaCreacion = DateTime.UtcNow
                    });

                    clavesExistentes.Add(clave);
                }

                if (nuevas.Any())
                    foreach (var n in nuevas)
                        await _notificacionRepository.Agregar(n);

                if (nuevas.Any() || hayCambiosEnExistentes)
                {
                    await _notificacionRepository.GuardarCambios();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando notificaciones de vencimiento para usuario {UsuarioId}", usuarioId);
            }
        }

        public async Task<IEnumerable<object>> ObtenerNotificacionesUsuarioAsync(int usuarioId, bool soloNoLeidas = false)
        {
            var notifs = await _notificacionRepository.ObtenerPorUsuarioAsync(usuarioId, soloNoLeidas);

            return notifs.Select(n => (object)new
            {
                id = n.Id,
                titulo = n.Titulo,
                mensaje = n.Mensaje,
                tipo = n.Tipo,
                urlDestino = n.UrlDestino,
                leida = n.Leida,
                fechaCreacion = n.FechaCreacion
            });
        }

        public async Task<int> ContarNoLeidasAsync(int usuarioId)
        {
            return await _notificacionRepository.ContarNoLeidasAsync(usuarioId);
        }

        public async Task MarcarComoLeidaAsync(int notificacionId, int usuarioId)
        {
            var notif = await _notificacionRepository.PrimeroODefaultAsync(
                n => n.Id == notificacionId && n.UsuarioDestinoId == usuarioId);

            if (notif != null)
            {
                notif.Leida = true;
                notif.FechaLectura = DateTime.UtcNow;
                await _notificacionRepository.Actualizar(notif);
                await _notificacionRepository.GuardarCambios();
            }
        }

        public async Task MarcarTodasComoLeidasAsync(int usuarioId)
        {
            var noLeidas = await _notificacionRepository.ObtenerPorUsuarioAsync(
                usuarioId, soloNoLeidas: true, limite: int.MaxValue);
            var lista = noLeidas.ToList();

            foreach (var n in lista)
            {
                n.Leida = true;
                n.FechaLectura = DateTime.UtcNow;
                await _notificacionRepository.Actualizar(n);
            }

            if (lista.Any())
                await _notificacionRepository.GuardarCambios();
        }

        public async Task<bool> EliminarNotificacionAsync(int notificacionId, int usuarioId)
        {
            try
            {
                var notif = await _notificacionRepository.PrimeroODefaultAsync(
                    n => n.Id == notificacionId && n.UsuarioDestinoId == usuarioId);

                if (notif == null)
                    return false;

                await _notificacionRepository.Eliminar(notificacionId);
                await _notificacionRepository.GuardarCambios();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando notificación {NotificacionId} del usuario {UsuarioId}", notificacionId, usuarioId);
                return false;
            }
        }

        public async Task EliminarTodasNotificacionesAsync(int usuarioId)
        {
            try
            {
                var todas = await _notificacionRepository.ObtenerPorUsuarioAsync(
                    usuarioId, soloNoLeidas: false, limite: int.MaxValue);
                var lista = todas.ToList();

                foreach (var n in lista)
                    await _notificacionRepository.Eliminar(n.Id);

                if (lista.Any())
                    await _notificacionRepository.GuardarCambios();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando todas las notificaciones del usuario {UsuarioId}", usuarioId);
            }
        }

        /// <summary>
        /// Retorna los usuarios activos con rol EDITOR que no tienen ninguna actividad asignada.
        /// </summary>
        public async Task<IEnumerable<object>> ObtenerUsuariosSinActividadesAsync()
        {
            var actividadesConResponsable = await _actividadRepository.ObtenerTodos();
            var idsConActividad = actividadesConResponsable
                .Select(a => a.FuncionariosResponsablesId)
                .Distinct()
                .ToHashSet();

            var editores = await _usuarioRepository.BuscarAsync(
                u => u.estado && u.Rol != null && u.Rol.nombre == "EDITOR");

            return editores
                .Where(u => !idsConActividad.Contains(u.Id_Usuario))
                .Select(u => (object)new
                {
                    id = u.Id_Usuario,
                    nombre = u.nombre,
                    correo = u.correo_electronico,
                    departamento = u.departamento
                });
        }

        /// <summary>
        /// Crea una notificación para cada ADMIN/SUPERADMIN sobre editores sin actividades.
        /// Evita duplicados: no crea si ya existe una notificación no leída del mismo tipo.
        /// </summary>
        public async Task NotificarAdminsSobreEditoresSinActividadesAsync()
        {
            try
            {
                var editoresSin = (await ObtenerUsuariosSinActividadesAsync()).ToList();

                if (editoresSin.Count == 0) return;

                var admins = await _usuarioRepository.BuscarAsync(
                    u => u.estado && u.Rol != null &&
                         (u.Rol.nombre == "ADMIN" || u.Rol.nombre == "SUPERADMIN"));

                if (!admins.Any()) return;

                var titulo = editoresSin.Count == 1
                    ? "1 usuario editor sin actividades asignadas"
                    : $"{editoresSin.Count} usuarios editores sin actividades asignadas";

                var mensaje = editoresSin.Count == 1
                    ? "Hay un editor sin ninguna actividad asignada. Revisá la gestión de usuarios."
                    : $"Hay {editoresSin.Count} editores sin ninguna actividad asignada. Revisá la gestión de usuarios.";

                foreach (var admin in admins)
                {
                    var yaExiste = await _notificacionRepository.ExisteNotificacionNoLeidaAsync(
                        admin.Id_Usuario, "editores-sin-actividades");

                    if (!yaExiste)
                    {
                        await CrearNotificacionAsync(
                            admin.Id_Usuario,
                            titulo,
                            mensaje,
                            "editores-sin-actividades",
                            "/users?sinActividades=true"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notificando admins sobre editores sin actividades");
            }
        }
    }
}