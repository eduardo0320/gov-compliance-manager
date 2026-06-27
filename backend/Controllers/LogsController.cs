using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/logs")]
    [Authorize(Policy = "AdminOrSuperadmin")]
    public class LogsController : ControllerBase
    {
        private readonly NormasDb _context;

        public LogsController(NormasDb context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> ObtenerLogs(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 20,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] string? tipoAccion = null,
            [FromQuery] int? usuarioId = null)
        {
            if (pagina < 1) pagina = 1;
            if (tamanoPagina < 1 || tamanoPagina > 200) tamanoPagina = 20;

            var query = _context.Auditorias
                .AsNoTracking()
                .Include(a => a.Usuario)
                .AsQueryable();

            if (fechaDesde.HasValue)
            {
                var inicio = fechaDesde.Value.Date;
                query = query.Where(a => a.FechaEvento >= inicio);
            }

            if (fechaHasta.HasValue)
            {
                var fin = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.FechaEvento <= fin);
            }

            if (!string.IsNullOrWhiteSpace(tipoAccion))
            {
                var tipo = tipoAccion.Trim();
                query = query.Where(a => a.TipoEvento == tipo);
            }

            if (usuarioId.HasValue)
            {
                query = query.Where(a => a.IdUsuario == usuarioId.Value);
            }

            query = query
                .OrderByDescending(a => a.FechaEvento)
                .ThenByDescending(a => a.IdAuditoria);

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanoPagina);

            if (totalPaginas == 0) totalPaginas = 1;
            if (pagina > totalPaginas) pagina = totalPaginas;

            var logs = await query
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .Select(a => new
                {
                    idAuditoria = a.IdAuditoria,
                    descripcion = a.Descripcion,
                    fechaEvento = a.FechaEvento,
                    tipoEvento = a.TipoEvento,
                    modulo = a.Modulo,
                    usuarioId = a.IdUsuario,
                    nombreUsuario = a.Usuario != null ? a.Usuario.nombre : "Sistema",
                    direccionIp = a.DireccionIp,
                    navegador = a.Navegador
                })
                .ToListAsync();

            return Ok(new
            {
                logs,
                paginaActual = pagina,
                tamanoPagina,
                totalRegistros,
                totalPaginas,
                orden = "desc"
            });
        }

        [HttpGet("filtros")]
        public async Task<ActionResult<object>> ObtenerFiltros()
        {
            var tiposAccion = await _context.Auditorias
                .AsNoTracking()
                .Where(a => !string.IsNullOrWhiteSpace(a.TipoEvento))
                .Select(a => a.TipoEvento)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            var idsConLogs = await _context.Auditorias
                .AsNoTracking()
                .Where(a => a.IdUsuario.HasValue)
                .Select(a => a.IdUsuario!.Value)
                .Distinct()
                .ToListAsync();

            var usuariosConocidos = await _context.Usuarios
                .AsNoTracking()
                .Where(u => idsConLogs.Contains(u.Id_Usuario))
                .Select(u => new { u.Id_Usuario, u.nombre })
                .ToListAsync();

            var nombresPorId = usuariosConocidos
                .ToDictionary(u => u.Id_Usuario, u => u.nombre);

            var usuarios = idsConLogs
                .Select(id => new UsuarioFiltroResponse
                {
                    idUsuario = id,
                    nombreUsuario = nombresPorId.TryGetValue(id, out var nombre)
                        ? nombre
                        : $"Usuario {id}"
                })
                .OrderBy(u => u.nombreUsuario)
                .ToList();

            return Ok(new
            {
                tiposAccion,
                usuarios
            });
        }

        private sealed class UsuarioFiltroResponse
        {
            public int idUsuario { get; set; }
            public string nombreUsuario { get; set; } = string.Empty;
        }
    }
}