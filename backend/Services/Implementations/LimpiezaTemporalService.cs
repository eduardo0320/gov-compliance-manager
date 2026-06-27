using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using backend.Config;

namespace backend.Services.Implementations
{
    /// <summary>
    /// Servicio en segundo plano que elimina archivos temporales
    /// (subidas incompletas o abandonadas) que superen la antigüedad
    /// configurada en DocumentosConfig.MantenimientoAutomatico.LimpiarTempCadaHoras.
    /// Se ejecuta una vez al inicio y luego con el intervalo configurado.
    /// </summary>
    public class LimpiezaTemporalService : BackgroundService
    {
        private readonly DocumentosConfig _config;
        private readonly ILogger<LimpiezaTemporalService> _logger;

        public LimpiezaTemporalService(
            IOptions<DocumentosConfig> config,
            ILogger<LimpiezaTemporalService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LimpiezaTemporalService iniciado.");

            // Espera inicial de 2 minutos para no interferir con el arranque del servidor
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            var intervalo = TimeSpan.FromHours(
                _config.MantenimientoAutomatico?.LimpiarTempCadaHoras ?? 24);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await LimpiarArchivosTemporalesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante limpieza de archivos temporales.");
                }

                await Task.Delay(intervalo, stoppingToken);
            }
        }

        private Task LimpiarArchivosTemporalesAsync()
        {
            var rutaTemp = _config.RutaTemporal;

            if (string.IsNullOrWhiteSpace(rutaTemp) || !Directory.Exists(rutaTemp))
            {
                _logger.LogDebug(
                    "Directorio temporal no existe o no configurado: {Ruta}", rutaTemp);
                return Task.CompletedTask;
            }

            var umbralHoras = _config.MantenimientoAutomatico?.LimpiarTempCadaHoras ?? 24;
            var corte = DateTime.UtcNow.AddHours(-umbralHoras);
            int eliminados = 0;
            int errores = 0;

            // Eliminar archivos individuales antiguos
            foreach (var archivo in Directory.EnumerateFiles(rutaTemp, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new FileInfo(archivo);
                    if (info.LastWriteTimeUtc < corte)
                    {
                        File.Delete(archivo);
                        eliminados++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo eliminar archivo temporal: {Archivo}", archivo);
                    errores++;
                }
            }

            // Eliminar directorios de sesión vacíos
            foreach (var dir in Directory.EnumerateDirectories(rutaTemp))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo eliminar directorio temporal: {Dir}", dir);
                }
            }

            if (eliminados > 0 || errores > 0)
                _logger.LogInformation(
                    "Limpieza temporal completada: {Eliminados} archivo(s) eliminado(s), {Errores} error(es).",
                    eliminados, errores);
            else
                _logger.LogDebug("Limpieza temporal: sin archivos que eliminar.");

            return Task.CompletedTask;
        }
    }
}
