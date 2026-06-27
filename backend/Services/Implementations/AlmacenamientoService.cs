using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using backend.Config;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class AlmacenamientoService : IAlmacenamientoService
    {
        private readonly DocumentosConfig _config;
        private readonly IActividadRepository _actividadRepo;
        private readonly ILogger<AlmacenamientoService> _logger;

        // Magic numbers para validar que el contenido coincide con la extensión declarada
        // DOCX, PPTX y XLSX son archivos ZIP → comparten los primeros 4 bytes (PK\x03\x04)
        // DOC y XLS son formato OLE2 Compound Document → 0xD0 0xCF 0x11 0xE0
        private static readonly Dictionary<string, byte[]> MagicNumbers = new()
        {
            { ".pdf",  [0x25, 0x50, 0x44, 0x46] }, // %PDF
            { ".docx", [0x50, 0x4B, 0x03, 0x04] }, // PK (ZIP)
            { ".pptx", [0x50, 0x4B, 0x03, 0x04] }, // PK (ZIP)
            { ".xlsx", [0x50, 0x4B, 0x03, 0x04] }, // PK (ZIP)
            { ".doc",  [0xD0, 0xCF, 0x11, 0xE0] }, // OLE2 Compound Document
            { ".xls",  [0xD0, 0xCF, 0x11, 0xE0] }  // OLE2 Compound Document
        };

        public AlmacenamientoService(
            IOptions<DocumentosConfig> config,
            IActividadRepository actividadRepo,
            ILogger<AlmacenamientoService> logger)
        {
            _config = config.Value;
            _actividadRepo = actividadRepo;
            _logger = logger;
        }

        // ──────────────────────────────────────────────
        //  Validación
        // ──────────────────────────────────────────────

        public async Task<(bool Valido, string? Error)> ValidarArchivoAsync(IFormFile archivo)
        {
            // 1. Extensión
            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!_config.ExtensionesPermitidas.Contains(ext))
                return (false, $"Extensión '{ext}' no permitida. Se aceptan: {string.Join(", ", _config.ExtensionesPermitidas)}");

            // 2. Tamaño
            var maxBytes = (long)_config.TamanoMaximoMB * 1024 * 1024;
            if (archivo.Length > maxBytes)
                return (false, $"El archivo supera el tamaño máximo de {_config.TamanoMaximoMB}MB");

            if (archivo.Length == 0)
                return (false, "El archivo está vacío");

            // 3. Magic numbers (solo si hay configuración de seguridad)
            if (_config.Seguridad.ValidarIntegridad && !await ValidarMagicNumbersAsync(archivo, ext))
                return (false, "El contenido del archivo no coincide con su extensión declarada");

            return (true, null);
        }

        private static async Task<bool> ValidarMagicNumbersAsync(IFormFile archivo, string extension)
        {
            if (!MagicNumbers.TryGetValue(extension, out var expectedBytes))
                return true; // extensión sin magic number registrado → se permite

            var buffer = new byte[expectedBytes.Length];
            using var stream = archivo.OpenReadStream();
            var bytesLeidos = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

            return bytesLeidos == expectedBytes.Length && buffer.SequenceEqual(expectedBytes);
        }

        // ──────────────────────────────────────────────
        //  Temporal
        // ──────────────────────────────────────────────

        public async Task<string> GuardarTemporalAsync(IFormFile archivo)
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var carpetaTemp = Path.Combine(_config.RutaTemporal, $"upload_{sessionId}");
            Directory.CreateDirectory(carpetaTemp);

            // Sanitizar el nombre original para evitar caracteres inválidos
            var nombreSanitizado = SanitizarNombreArchivo(archivo.FileName);
            var rutaArchivo = Path.Combine(carpetaTemp, nombreSanitizado);

            using var fileStream = new FileStream(rutaArchivo, FileMode.Create, FileAccess.Write);
            await archivo.CopyToAsync(fileStream);

            _logger.LogDebug("Archivo guardado en temporal: {Ruta}", rutaArchivo);
            return rutaArchivo;
        }

        public async Task LimpiarTemporalAsync(string rutaTemporal)
        {
            await Task.CompletedTask;
            try
            {
                var carpeta = Path.GetDirectoryName(rutaTemporal);
                if (carpeta != null && Directory.Exists(carpeta))
                {
                    Directory.Delete(carpeta, recursive: true);
                    _logger.LogDebug("Carpeta temporal eliminada: {Carpeta}", carpeta);
                }
            }
            catch (Exception ex)
            {
                // No propagar el error — la limpieza no debe interrumpir el flujo principal
                _logger.LogWarning(ex, "No se pudo limpiar la carpeta temporal: {Ruta}", rutaTemporal);
            }
        }

        // ──────────────────────────────────────────────
        //  Repositorio
        // ──────────────────────────────────────────────

        public async Task<string> MoverARepositorioAsync(
            string rutaTemporal,
            int actividadId,
            int documentoId,
            int numeroVersion,
            string nombreArchivoOriginal)
        {
            // Obtener la jerarquía completa para construir la ruta
            var actividad = await _actividadRepo.ObtenerPorIdConJerarquia(actividadId);
            if (actividad == null)
                throw new KeyNotFoundException($"Actividad {actividadId} no encontrada al construir ruta de almacenamiento");

            var subdominio = actividad.Subdominio;
            var proceso = subdominio.Proceso;
            var dominio = proceso.Dominio;

            var max = _config.EstructuraJerarquica.MaxCaracteresNombre;

            // Construir la ruta relativa jerárquica
            var segmentoRelativo = Path.Combine(
                "Dominios",
                $"dom_{dominio.IdDominio:D3}_{SanitizarNombre(dominio.Nombre, max)}",
                $"proc_{proceso.IdProceso:D3}_{SanitizarNombre(proceso.Nombre, max)}",
                $"sub_{subdominio.IdSubdominio:D3}_{SanitizarNombre(subdominio.PracticasGobierno, max)}",
                $"act_{actividad.IdActividad:D3}_{SanitizarNombre(actividad.Nombre, max)}",
                $"doc_{documentoId:D4}"
            );

            var carpetaAbsoluta = Path.Combine(_config.RutaRepositorio, segmentoRelativo);
            Directory.CreateDirectory(carpetaAbsoluta);

            // Construir nombre de archivo: v{N}_{nombreOriginalSanitizado}
            var extension = Path.GetExtension(nombreArchivoOriginal);
            var baseName = Path.GetFileNameWithoutExtension(nombreArchivoOriginal);
            var nombreVersion = $"v{numeroVersion}_{SanitizarNombre(baseName, 100)}{extension}";

            var rutaRelativa = Path.Combine(segmentoRelativo, nombreVersion);
            var rutaAbsoluta = Path.Combine(_config.RutaRepositorio, rutaRelativa);

            // Defensa anti path traversal
            var canonico = Path.GetFullPath(rutaAbsoluta);
            var raizCanonica = Path.GetFullPath(_config.RutaRepositorio);
            if (!canonico.StartsWith(raizCanonica + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !canonico.Equals(raizCanonica, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Intento de path traversal detectado");

            File.Move(rutaTemporal, canonico, overwrite: false);
            _logger.LogInformation("Archivo movido al repositorio: {Ruta}", rutaRelativa);

            return rutaRelativa;
        }

        public string ObtenerRutaCompleta(string rutaRelativa)
            => Path.Combine(_config.RutaRepositorio, rutaRelativa);

        public bool ArchivoExiste(string rutaRelativa)
            => File.Exists(ObtenerRutaCompleta(rutaRelativa));

        // ──────────────────────────────────────────────
        //  Sanitización de nombres
        // ──────────────────────────────────────────────

        private static string SanitizarNombre(string nombre, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "sin_nombre";

            // Descomponer caracteres Unicode y eliminar marcas diacríticas (acentos, tilde, etc.)
            var normalizado = nombre.Normalize(NormalizationForm.FormD);
            var sinAcentos = new string(
                normalizado
                    .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    .ToArray()
            );

            // Espacios → guión bajo, eliminar caracteres no válidos para nombres de directorio
            sinAcentos = sinAcentos.Replace(' ', '_');
            sinAcentos = string.Concat(sinAcentos.Split(Path.GetInvalidFileNameChars()));

            // Solo letras, dígitos y guión bajo
            sinAcentos = new string(sinAcentos.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            if (sinAcentos.Length > maxLength)
                sinAcentos = sinAcentos[..maxLength];

            return string.IsNullOrEmpty(sinAcentos) ? "sin_nombre" : sinAcentos;
        }

        private static string SanitizarNombreArchivo(string nombreOriginal)
        {
            var nombre = Path.GetFileNameWithoutExtension(nombreOriginal);
            var ext = Path.GetExtension(nombreOriginal);
            return SanitizarNombre(nombre, 200) + ext;
        }
    }
}
