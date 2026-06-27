using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/notificaciones")]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionService _notificacionService;

        public NotificacionesController(INotificacionService notificacionService)
        {
            _notificacionService = notificacionService;
        }

        private int GetUsuarioId()
        {
            var sid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(sid, out var id) ? id : 0;
        }

        /// <summary>
        /// GET /api/notificaciones — Obtiene notificaciones del usuario autenticado
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> ObtenerMisNotificaciones([FromQuery] bool soloNoLeidas = false)
        {
            var usuarioId = GetUsuarioId();
            if (usuarioId == 0) return Unauthorized();

            await _notificacionService.GenerarNotificacionesVencimientoActividadesUsuarioAsync(usuarioId);

            var notificaciones = await _notificacionService.ObtenerNotificacionesUsuarioAsync(usuarioId, soloNoLeidas);
            var noLeidas = await _notificacionService.ContarNoLeidasAsync(usuarioId);

            return Ok(new { notificaciones, noLeidas });
        }

        /// <summary>
        /// PUT /api/notificaciones/{id}/leer — Marca una notificación como leída
        /// </summary>
        [HttpPut("{id:int}/leer")]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            var usuarioId = GetUsuarioId();
            if (usuarioId == 0) return Unauthorized();

            await _notificacionService.MarcarComoLeidaAsync(id, usuarioId);
            return Ok(new { mensaje = "Notificación marcada como leída" });
        }

        /// <summary>
        /// PUT /api/notificaciones/leer-todas — Marca todas las notificaciones como leídas
        /// </summary>
        [HttpPut("leer-todas")]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var usuarioId = GetUsuarioId();
            if (usuarioId == 0) return Unauthorized();

            await _notificacionService.MarcarTodasComoLeidasAsync(usuarioId);
            return Ok(new { mensaje = "Todas las notificaciones marcadas como leídas" });
        }

        /// <summary>
        /// DELETE /api/notificaciones/{id} — Elimina una notificación del usuario autenticado
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarNotificacion(int id)
        {
            var usuarioId = GetUsuarioId();
            if (usuarioId == 0) return Unauthorized();

            var eliminada = await _notificacionService.EliminarNotificacionAsync(id, usuarioId);
            if (!eliminada)
                return NotFound(new { mensaje = "Notificación no encontrada o no pertenece al usuario" });

            return Ok(new { mensaje = "Notificación eliminada" });
        }

        /// <summary>
        /// DELETE /api/notificaciones/eliminar-todas — Elimina todas las notificaciones del usuario
        /// </summary>
        [HttpDelete("eliminar-todas")]
        public async Task<IActionResult> EliminarTodasNotificaciones()
        {
            var usuarioId = GetUsuarioId();
            if (usuarioId == 0) return Unauthorized();

            await _notificacionService.EliminarTodasNotificacionesAsync(usuarioId);
            return Ok(new { mensaje = "Todas las notificaciones eliminadas" });
        }

        /// <summary>
        /// GET /api/notificaciones/usuarios-sin-actividades — Solo ADMIN/SUPERADMIN
        /// Retorna editores activos sin ninguna actividad asignada.
        /// </summary>
        [HttpGet("usuarios-sin-actividades")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<ActionResult<object>> ObtenerUsuariosSinActividades()
        {
            var usuarios = await _notificacionService.ObtenerUsuariosSinActividadesAsync();
            var lista = ((IEnumerable<object>)usuarios).ToList();
            return Ok(new { usuarios = lista, total = lista.Count });
        }

        /// <summary>
        /// POST /api/notificaciones/notificar-editores-sin-actividades — Solo ADMIN/SUPERADMIN
        /// Genera notificaciones persistentes en BD para todos los admins si hay editores sin actividades.
        /// </summary>
        [HttpPost("notificar-editores-sin-actividades")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> NotificarEditoresSinActividades()
        {
            await _notificacionService.NotificarAdminsSobreEditoresSinActividadesAsync();
            return Ok(new { mensaje = "Notificaciones generadas correctamente" });
        }
    }

}