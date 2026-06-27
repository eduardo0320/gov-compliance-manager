using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Services.Implementations
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly NormasDb _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(
            NormasDb context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditoriaService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task RegistrarEventoAsync(
            string tipoEvento,
            string descripcion,
            string? modulo = null,
            int? usuarioId = null,
            object? datosAnteriores = null,
            object? datosNuevos = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var usuarioIdFinal = usuarioId ?? ObtenerUsuarioIdDelContexto(httpContext);

                var auditoria = new Auditoria
                {
                    TipoEvento = LimitarTexto(tipoEvento, 100) ?? "Evento",
                    Descripcion = LimitarTexto(descripcion, 500) ?? "Evento de auditoría",
                    Modulo = LimitarTexto(modulo, 100),
                    IdUsuario = usuarioIdFinal,
                    FechaEvento = DateTime.UtcNow,
                    DireccionIp = LimitarTexto(httpContext?.Connection?.RemoteIpAddress?.ToString(), 50),
                    Navegador = LimitarTexto(httpContext?.Request?.Headers.UserAgent.ToString(), 500),
                    DatosAnteriores = SerializarDatos(datosAnteriores),
                    DatosNuevos = SerializarDatos(datosNuevos)
                };

                _context.Auditorias.Add(auditoria);
                await _context.SaveChangesAsync();

                _logger.LogDebug(
                    "[Auditoria] {TipoEvento} - {Descripcion}",
                    auditoria.TipoEvento,
                    auditoria.Descripcion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al registrar evento de auditoría. TipoEvento={TipoEvento}, Descripcion={Descripcion}",
                    tipoEvento,
                    descripcion);
            }
        }

        public void Actualizar(string mensaje, Auditoria datos)
        {
            try
            {
                _context.Auditorias.Add(datos);
                _context.SaveChanges();

                _logger.LogDebug(
                    "[AuditoriaObserver] {TipoEvento} - {Descripcion}",
                    datos.TipoEvento,
                    datos.Descripcion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al persistir evento de auditoría [{Mensaje}] {TipoEvento}",
                    mensaje, datos.TipoEvento);
            }
        }

        private static int? ObtenerUsuarioIdDelContexto(HttpContext? httpContext)
        {
            var claimId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claimId, out var usuarioId) ? usuarioId : null;
        }

        private static string? SerializarDatos(object? datos)
        {
            if (datos == null)
                return null;

            return JsonSerializer.Serialize(datos, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }

        private static string? LimitarTexto(string? valor, int maximo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            var limpio = valor.Trim();
            return limpio.Length <= maximo ? limpio : limpio[..maximo];
        }
    }
}

