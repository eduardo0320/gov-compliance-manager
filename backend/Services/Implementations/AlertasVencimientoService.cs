using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    /// <summary>
    /// Servicio en segundo plano que envía alertas por email cuando un documento
    /// alcanza su FechaAlerta. Se ejecuta cada 24 horas y notifica al creador del documento.
    /// Sólo notifica documentos cuya FechaAlerta está dentro de los últimos 7 días
    /// (ventana deslizante para evitar spam en reinicios).
    /// </summary>
    public class AlertasVencimientoService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AlertasVencimientoService> _logger;

        public AlertasVencimientoService(
            IServiceScopeFactory scopeFactory,
            ILogger<AlertasVencimientoService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertasVencimientoService iniciado.");
            var horaEjecucion = new TimeSpan(17, 30, 0);

            while (!stoppingToken.IsCancellationRequested)
            {
                var espera = CalcularEsperaHastaProximaEjecucion(horaEjecucion);
                var siguienteEjecucion = DateTime.Now.Add(espera);
                _logger.LogInformation(
                    "Próxima ejecución de alertas programada para: {SiguienteEjecucion}",
                    siguienteEjecucion);

                await Task.Delay(espera, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await EnviarAlertasPendientesAsync();
                    await EnviarAlertasActividadesPendientesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar alertas de vencimiento.");
                }
            }
        }

        private async Task EnviarAlertasPendientesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<NormasDb>();
            var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var hoy = DateTime.Today;

            // Notificar documentos cuya FechaAlerta cayó en los últimos 7 días
            // (así si el servidor estuvo caído no omite notificaciones recientes)
            var ventanaAtras = hoy.AddDays(-7);

            var documentos = await context.Documentos
                .Include(d => d.CreadoPor)
                .Include(d => d.Actividad)
                .Where(d => !d.Eliminado
                    && d.FechaAlerta.HasValue
                    && d.FechaAlerta.Value.Date >= ventanaAtras
                    && d.FechaAlerta.Value.Date <= hoy
                    && d.Estado != "Obsoleto"
                    && d.Estado != "Archivado")
                .ToListAsync();

            _logger.LogInformation(
                "AlertasVencimientoService: {Count} documento(s) a notificar.", documentos.Count);

            foreach (var doc in documentos)
            {
                var usuario = doc.CreadoPor;
                if (usuario == null || string.IsNullOrWhiteSpace(usuario.correo_electronico))
                    continue;

                var diasRestantes = doc.FechaVencimiento.HasValue
                    ? (int)(doc.FechaVencimiento.Value.Date - hoy).TotalDays
                    : 0;

                await emailSvc.EnviarAlertaVencimientoDocumento(
                    correo: usuario.correo_electronico,
                    nombre: usuario.nombre,
                    nombreDocumento: doc.Nombre,
                    diasRestantes: diasRestantes,
                    actividadNombre: doc.Actividad?.Nombre ?? "—");
            }
        }

        private async Task EnviarAlertasActividadesPendientesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var actividadService = scope.ServiceProvider.GetRequiredService<IActividadService>();

            var resultado = await actividadService.enviarCorreosVencimientoActividadesAsync();
            var payload = resultado is null
                ? "null"
                : System.Text.Json.JsonSerializer.Serialize(resultado);

            _logger.LogInformation(
                "AlertasVencimientoService (actividades): {Resultado}",
                payload);
        }

        private static TimeSpan CalcularEsperaHastaProximaEjecucion(TimeSpan horaObjetivo)
        {
            var ahora = DateTime.Now;
            var proximaEjecucion = ahora.Date.Add(horaObjetivo);

            if (ahora >= proximaEjecucion)
                proximaEjecucion = proximaEjecucion.AddDays(1);

            return proximaEjecucion - ahora;
        }
    }
}
