using Microsoft.AspNetCore.Http;

namespace backend.Services.Interfaces
{
    public interface IAlmacenamientoService
    {
        /// <summary>
        /// Valida extensión, tamaño y magic numbers del archivo.
        /// Retorna (true, null) si es válido, o (false, mensajeError) si no.
        /// </summary>
        Task<(bool Valido, string? Error)> ValidarArchivoAsync(IFormFile archivo);

        /// <summary>
        /// Guarda el archivo en la carpeta Temp con un ID de sesión único.
        /// Retorna la ruta absoluta del archivo temporal.
        /// </summary>
        Task<string> GuardarTemporalAsync(IFormFile archivo);

        /// <summary>
        /// Mueve el archivo desde Temp a la estructura jerárquica del repositorio.
        /// Retorna la ruta relativa al directorio raíz del repositorio (se persiste en BD).
        /// </summary>
        Task<string> MoverARepositorioAsync(
            string rutaTemporal,
            int actividadId,
            int documentoId,
            int numeroVersion,
            string nombreArchivoOriginal);

        /// <summary>Elimina la carpeta temporal de la sesión de subida.</summary>
        Task LimpiarTemporalAsync(string rutaTemporal);

        /// <summary>Resuelve la ruta absoluta de un archivo a partir de su ruta relativa almacenada en BD.</summary>
        string ObtenerRutaCompleta(string rutaRelativa);

        /// <summary>Verifica que el archivo físico exista en el repositorio.</summary>
        bool ArchivoExiste(string rutaRelativa);
    }
}
