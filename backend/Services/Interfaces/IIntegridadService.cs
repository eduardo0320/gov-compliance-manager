using Microsoft.AspNetCore.Http;

namespace backend.Services.Interfaces
{
    public interface IIntegridadService
    {
        /// <summary>Calcula el hash SHA-256 del contenido de un IFormFile.</summary>
        Task<string> CalcularChecksumAsync(IFormFile archivo);

        /// <summary>Calcula el hash SHA-256 desde un Stream abierto.</summary>
        Task<string> CalcularChecksumAsync(Stream stream);

        /// <summary>
        /// Verifica la integridad de una versión física comparando su checksum almacenado
        /// con el checksum calculado del archivo actualmente en disco.
        /// </summary>
        Task<bool> ValidarIntegridadAsync(int versionDocumentoId);
    }
}
