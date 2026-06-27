using Microsoft.Extensions.Options;
using backend.Config;

namespace backend.Services.Implementations
{
    /// <summary>
    /// Servicio en segundo plano que realiza copias de seguridad del repositorio de
    /// documentos al directorio de backups configurado en DocumentosConfig.
    /// La primera ejecución ocurre a las 12:00 a.m. (hora local) y luego se repite
    /// cada IntervaloBackupDias (default 7 días).
    /// Los backups más antiguos que EliminarBackupsAntiguosDias se eliminan automáticamente.
    /// </summary>
    public class BackupService : BackgroundService
    {
        private readonly DocumentosConfig _config;
        private readonly ILogger<BackupService> _logger;

        public BackupService(
            IOptions<DocumentosConfig> config,
            ILogger<BackupService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "BackupService iniciado. Habilitado={Habilitado}, IntervaloDias={Dias}",
                _config.HabilitarBackupAutomatico,
                _config.IntervaloBackupDias);

            if (!_config.HabilitarBackupAutomatico)
            {
                _logger.LogInformation("BackupService deshabilitado en configuración. Terminando.");
                return;
            }

            var horaEjecucion = new TimeSpan(0, 0, 0); // 12:00 a.m.
            var intervaloDias = _config.IntervaloBackupDias > 0 ? _config.IntervaloBackupDias : 7;

            var proximaEjecucion = DateTime.Now.Date.Add(horaEjecucion);
            if (DateTime.Now >= proximaEjecucion)
                proximaEjecucion = proximaEjecucion.AddDays(1);

            while (!stoppingToken.IsCancellationRequested)
            {
                var espera = proximaEjecucion - DateTime.Now;
                if (espera < TimeSpan.Zero)
                    espera = TimeSpan.Zero;

                _logger.LogInformation(
                    "Próximo backup programado para: {SiguienteEjecucion}",
                    proximaEjecucion);

                await Task.Delay(espera, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await RealizarBackupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante el backup del repositorio de documentos.");
                }

                proximaEjecucion = proximaEjecucion.AddDays(intervaloDias);
            }
        }

        // ──────────────────────────────────────────────────────────────────────

        private Task RealizarBackupAsync()
        {
            var origen = _config.RutaRepositorio;
            var destino = _config.RutaBackups;

            if (string.IsNullOrWhiteSpace(origen) || !Directory.Exists(origen))
            {
                _logger.LogWarning("Repositorio de origen no encontrado o no configurado: {Ruta}", origen);
                return Task.CompletedTask;
            }

            if (string.IsNullOrWhiteSpace(destino))
            {
                _logger.LogWarning("Ruta de backup no configurada (RutaBackups está vacía).");
                return Task.CompletedTask;
            }

            var nombreBackup = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var rutaBackup = Path.Combine(destino, nombreBackup);

            _logger.LogInformation("Iniciando backup: {Origen} → {Destino}", origen, rutaBackup);

            Directory.CreateDirectory(rutaBackup);
            CopiarDirectorioRecursivo(origen, rutaBackup);

            _logger.LogInformation("Backup completado en: {Destino}", rutaBackup);

            LimpiarBackupsAntiguos(destino);

            return Task.CompletedTask;
        }

        private static void CopiarDirectorioRecursivo(string origen, string destino)
        {
            foreach (var archivo in Directory.EnumerateFiles(origen, "*", SearchOption.AllDirectories))
            {
                var relativa = Path.GetRelativePath(origen, archivo);
                var rutaDestino = Path.Combine(destino, relativa);
                Directory.CreateDirectory(Path.GetDirectoryName(rutaDestino)!);
                File.Copy(archivo, rutaDestino, overwrite: true);
            }
        }

        private void LimpiarBackupsAntiguos(string rutaBackups)
        {
            var limite = _config.MantenimientoAutomatico?.EliminarBackupsAntiguosDias ?? 90;
            var corte = DateTime.UtcNow.AddDays(-limite);
            int eliminados = 0;

            foreach (var dir in Directory.EnumerateDirectories(rutaBackups))
            {
                try
                {
                    var info = new DirectoryInfo(dir);
                    if (info.CreationTimeUtc < corte)
                    {
                        Directory.Delete(dir, recursive: true);
                        eliminados++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo eliminar backup antiguo: {Dir}", dir);
                }
            }

            if (eliminados > 0)
                _logger.LogInformation(
                    "BackupService: eliminados {N} backup(s) con más de {Dias} días.", eliminados, limite);
        }

    }
}
