using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class IntegridadService : IIntegridadService
    {
        private readonly IVersionDocumentoRepository _versionRepo;
        private readonly IAlmacenamientoService _almacenamientoService;
        private readonly ILogger<IntegridadService> _logger;

        public IntegridadService(
            IVersionDocumentoRepository versionRepo,
            IAlmacenamientoService almacenamientoService,
            ILogger<IntegridadService> logger)
        {
            _versionRepo = versionRepo;
            _almacenamientoService = almacenamientoService;
            _logger = logger;
        }

        public async Task<string> CalcularChecksumAsync(IFormFile archivo)
        {
            using var sha256 = SHA256.Create();
            using var stream = archivo.OpenReadStream();
            var hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public async Task<string> CalcularChecksumAsync(Stream stream)
        {
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public async Task<bool> ValidarIntegridadAsync(int versionDocumentoId)
        {
            var version = await _versionRepo.ObtenerPorId(versionDocumentoId);

            if (version == null)
            {
                _logger.LogWarning("Versión {Id} no encontrada al validar integridad", versionDocumentoId);
                return false;
            }

            // Las versiones tipo URL no tienen archivo físico que validar
            if (version.TipoAlmacenamiento == "URL" || string.IsNullOrEmpty(version.RutaArchivo))
                return true;

            if (string.IsNullOrEmpty(version.ChecksumSHA256))
            {
                _logger.LogWarning("Versión {Id} no tiene checksum almacenado", versionDocumentoId);
                return false;
            }

            if (!_almacenamientoService.ArchivoExiste(version.RutaArchivo))
            {
                _logger.LogError("Archivo físico no encontrado para versión {Id}: {Ruta}",
                    versionDocumentoId, version.RutaArchivo);
                return false;
            }

            var rutaCompleta = _almacenamientoService.ObtenerRutaCompleta(version.RutaArchivo);
            using var stream = File.OpenRead(rutaCompleta);
            var checksumActual = await CalcularChecksumAsync(stream);

            var coincide = checksumActual.Equals(version.ChecksumSHA256, StringComparison.OrdinalIgnoreCase);

            if (!coincide)
                _logger.LogError(
                    "¡Corrupción detectada en versión {Id}! Esperado: {Esperado} | Actual: {Actual}",
                    versionDocumentoId, version.ChecksumSHA256, checksumActual);

            return coincide;
        }
    }
}
