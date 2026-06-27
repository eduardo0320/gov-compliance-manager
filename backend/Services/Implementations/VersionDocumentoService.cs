using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class VersionDocumentoService : IVersionDocumentoService
    {
        private readonly NormasDb _context;
        private readonly IVersionDocumentoRepository _versionRepo;
        private readonly IDocumentoRepository _documentoRepo;
        private readonly IAlmacenamientoService _almacenamientoService;
        private readonly IIntegridadService _integridadService;
        private readonly IHistorialActividadService _historialActividadService;
        private readonly ILogger<VersionDocumentoService> _logger;

        public VersionDocumentoService(
            NormasDb context,
            IVersionDocumentoRepository versionRepo,
            IDocumentoRepository documentoRepo,
            IAlmacenamientoService almacenamientoService,
            IIntegridadService integridadService,
            IHistorialActividadService historialActividadService,
            ILogger<VersionDocumentoService> logger)
        {
            _context = context;
            _versionRepo = versionRepo;
            _documentoRepo = documentoRepo;
            _almacenamientoService = almacenamientoService;
            _integridadService = integridadService;
            _historialActividadService = historialActividadService;
            _logger = logger;
        }

        // ──────────────────────────────────────────────
        //  Listado
        // ──────────────────────────────────────────────

        public async Task<IEnumerable<object>> ObtenerVersionesAsync(int documentoId)
        {
            // Verificar que el documento exista y no esté eliminado
            var documento = await _documentoRepo.ObtenerConVersionActual(documentoId);
            if (documento == null)
                throw new KeyNotFoundException($"Documento {documentoId} no encontrado");

            var versiones = await _versionRepo.ObtenerPorDocumentoId(documentoId);
            var versionActualId = documento.VersionActualId;

            return versiones.Select(v => MapVersionToObject(v, versionActualId));
        }

        // ──────────────────────────────────────────────
        //  Subir nueva versión
        // ──────────────────────────────────────────────

        public async Task<object> SubirNuevaVersionAsync(int documentoId, SubirVersionDto dto, int usuarioId)
        {
            // Validaciones de entrada
            if (dto.Archivo == null && string.IsNullOrWhiteSpace(dto.Url))
                throw new ArgumentException("Debe proporcionar un archivo o una URL");

            var documento = await _documentoRepo.ObtenerConVersionActual(documentoId);
            if (documento == null)
                throw new KeyNotFoundException($"Documento {documentoId} no encontrado");

            await ValidarActividadImplementableParaEdicionAsync(documento.ActividadId);

            // Verificar que el estado permite nuevas versiones
            if (documento.Estado is "Obsoleto" or "Archivado")
                throw new InvalidOperationException(
                    $"No se puede subir una versión a un documento en estado '{documento.Estado}'");

            string? rutaTemporal = null;
            string? checksum = null;

            try
            {
                if (dto.Archivo != null)
                {
                    // Validar archivo
                    var (valido, error) = await _almacenamientoService.ValidarArchivoAsync(dto.Archivo);
                    if (!valido) throw new ArgumentException(error);

                    // Guardar temporal para calcular checksum desde el archivo en disco
                    rutaTemporal = await _almacenamientoService.GuardarTemporalAsync(dto.Archivo);

                    using (var fs = File.OpenRead(rutaTemporal))
                        checksum = await _integridadService.CalcularChecksumAsync(fs);

                    // Detectar archivo idéntico a una versión existente
                    var existente = await _versionRepo.BuscarPorChecksum(documentoId, checksum);
                    if (existente != null)
                    {
                        await _almacenamientoService.LimpiarTemporalAsync(rutaTemporal);
                        rutaTemporal = null;
                        throw new InvalidOperationException(
                            "Este archivo es idéntico a una versión existente. No se creó una nueva versión.");
                    }
                }

                // Transacción BD
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var numeroVersion = await _versionRepo.ObtenerSiguienteNumeroVersion(documentoId);

                    // Calcular el texto de versión (major.minor) basándose en la versión actual
                    var tipoVersionamiento = (dto.TipoVersionamiento ?? "menor").ToLowerInvariant().Trim();
                    var nuevoVersionTexto  = ComputarVersionTexto(
                        documento.VersionActual?.VersionTexto ?? "1.0", tipoVersionamiento);

                    // Mover archivo al repositorio (si hay archivo físico)
                    string? rutaFinal = null;
                    if (dto.Archivo != null && rutaTemporal != null)
                    {
                        rutaFinal = await _almacenamientoService.MoverARepositorioAsync(
                            rutaTemporal,
                            documento.ActividadId,
                            documentoId,
                            numeroVersion,
                            dto.Archivo.FileName);

                        rutaTemporal = null; // archivo ya movido, no limpiar temp
                    }

                    var version = new VersionDocumento
                    {
                        DocumentoId = documentoId,
                        NumeroVersion = numeroVersion,
                        VersionTexto = nuevoVersionTexto,
                        TipoAlmacenamiento = dto.Archivo != null ? "Archivo" : "URL",
                        RutaArchivo = rutaFinal,
                        Url = dto.Url,
                        NombreArchivoOriginal = dto.Archivo?.FileName,
                        TamanoBytes = dto.Archivo?.Length,
                        MimeType = dto.Archivo?.ContentType,
                        ChecksumSHA256 = checksum,
                        Comentario = dto.Comentario,
                        FechaVencimiento = dto.FechaVencimiento,
                        SubidoPorId = usuarioId,
                        FechaSubida = DateTime.UtcNow,
                        Activo = true
                    };

                    await _versionRepo.Agregar(version);
                    await _context.SaveChangesAsync();

                    // Actualizar VersionActualId del documento y sincronizar vencimiento base
                    documento.VersionActualId = version.IdVersionDocumento;
                    documento.FechaVencimiento = dto.FechaVencimiento;
                    documento.ModificadoPorId = usuarioId;
                    documento.FechaModificacion = DateTime.UtcNow;
                    _context.Update(documento);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Versión {NumVersion} subida al documento {DocId} por usuario {UsrId}",
                        numeroVersion, documentoId, usuarioId);

                    await _historialActividadService.RegistrarVersionAnteriorAsync(
                        documento.ActividadId,
                        "Documentos: se subio una nueva version de documento",
                        usuarioId);

                    return MapVersionToObject(version, version.IdVersionDocumento);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch
            {
                if (rutaTemporal != null)
                    await _almacenamientoService.LimpiarTemporalAsync(rutaTemporal);
                throw;
            }
        }

        private async Task ValidarActividadImplementableParaEdicionAsync(int actividadId)
        {
            var actividad = await _context.Actividades
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.IdActividad == actividadId);

            if (actividad == null)
                throw new KeyNotFoundException($"Actividad {actividadId} no encontrada");

            if (string.Equals(actividad.Implementable, "No", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "La actividad esta marcada como no implementable. Los documentos solo pueden verse y descargarse.");
            }
        }

        // ──────────────────────────────────────────────
        //  Descarga
        // ──────────────────────────────────────────────

        public async Task<(Stream Contenido, string NombreArchivo, string ContentType)?> DescargarVersionAsync(
            int documentoId, int? numeroVersion = null)
        {
            VersionDocumento? version;

            if (numeroVersion.HasValue)
            {
                version = await _versionRepo.ObtenerPorDocumentoIdYNumeroVersion(documentoId, numeroVersion.Value);
            }
            else
            {
                var documento = await _documentoRepo.ObtenerConVersionActual(documentoId);
                version = documento?.VersionActual;
            }

            if (version == null)
                return null;

            // Las URLs externas no se sirven como stream
            if (version.TipoAlmacenamiento == "URL" || string.IsNullOrEmpty(version.RutaArchivo))
                return null;

            if (!_almacenamientoService.ArchivoExiste(version.RutaArchivo))
            {
                _logger.LogError(
                    "Archivo físico ausente al intentar descarga — versión {Id}: {Ruta}",
                    version.IdVersionDocumento, version.RutaArchivo);
                return null;
            }

            var rutaCompleta = _almacenamientoService.ObtenerRutaCompleta(version.RutaArchivo);
            var stream = File.OpenRead(rutaCompleta);
            var nombreArchivo = version.NombreArchivoOriginal
                ?? Path.GetFileName(version.RutaArchivo);
            var contentType = version.MimeType ?? "application/octet-stream";

            return (stream, nombreArchivo, contentType);
        }

        // ──────────────────────────────────────────────
        //  Mapeo
        // ──────────────────────────────────────────────

        private static object MapVersionToObject(VersionDocumento v, int? versionActualId) => new
        {
            id = v.IdVersionDocumento,
            documentoId = v.DocumentoId,
            numeroVersion = v.NumeroVersion,
            versionTexto = v.VersionTexto,
            esVersionActual = v.IdVersionDocumento == versionActualId,
            tipoAlmacenamiento = v.TipoAlmacenamiento,
            url = v.Url,
            nombreArchivoOriginal = v.NombreArchivoOriginal,
            tamanoBytes = v.TamanoBytes,
            mimeType = v.MimeType,
            comentario = v.Comentario,
            fechaVencimiento = v.FechaVencimiento,
            subidoPorId = v.SubidoPorId,
            subidoPorNombre = v.SubidoPor?.nombre,
            fechaSubida = v.FechaSubida,
            activo = v.Activo
        };

        // ────────────────────────────────────────────
        //  Utilidades de versiones
        // ────────────────────────────────────────────

        /// <summary>
        /// Calcula el texto de la próxima versión.
        /// tipoVersionamiento="menor": 1.0 → 1.1, 1.3 → 1.4
        /// tipoVersionamiento="mayor": 1.0 → 2.0, 1.3 → 2.0
        /// </summary>
        private static string ComputarVersionTexto(string versionActual, string tipoVersionamiento)
        {
            var partes = versionActual.Split('.');
            int mayor  = partes.Length > 0 && int.TryParse(partes[0], out var m) ? m : 1;
            int menor  = partes.Length > 1 && int.TryParse(partes[1], out var mi) ? mi : 0;

            return tipoVersionamiento == "mayor"
                ? $"{mayor + 1}.0"
                : $"{mayor}.{menor + 1}";
        }
    }
}
