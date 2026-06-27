using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace backend.Services.Implementations
{
    public class DocumentoService : IDocumentoService
    {
        private readonly NormasDb _context;
        private readonly IDocumentoRepository _documentoRepo;
        private readonly IVersionDocumentoRepository _versionRepo;
        private readonly IRelacionDocumentoRepository _relacionRepo;
        private readonly IActividadRepository _actividadRepo;
        private readonly IDominioRepository _dominioRepo;
        private readonly IProcesoRepository _procesoRepo;
        private readonly ISubdominioRepository _subdominioRepo;
        private readonly IAlmacenamientoService _almacenamientoService;
        private readonly IIntegridadService _integridadService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IHistorialActividadService _historialActividadService;
        private readonly ILogger<DocumentoService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        // Transiciones de estado válidas:
        // Borrador     → En_Revision, Vigente
        // En_Revision  → Aprobado, Borrador
        // Aprobado     → Vigente, En_Revision
        // Vigente      → Obsoleto, Archivado
        // Obsoleto     → Archivado
        // Archivado    → (ninguna)
        private static readonly Dictionary<string, string[]> TransicionesValidas = new()
        {
            { "Borrador",    ["En_Revision", "Vigente"] },
            { "En_Revision", ["Aprobado", "Borrador"] },
            { "Aprobado",    ["Vigente", "En_Revision"] },
            { "Vigente",     ["Obsoleto", "Archivado"] },
            { "Obsoleto",    ["Archivado"] },
            { "Archivado",   [] }
        };

        public DocumentoService(
            NormasDb context,
            IDocumentoRepository documentoRepo,
            IVersionDocumentoRepository versionRepo,
            IRelacionDocumentoRepository relacionRepo,
            IActividadRepository actividadRepo,
            IDominioRepository dominioRepo,
            IProcesoRepository procesoRepo,
            ISubdominioRepository subdominioRepo,
            IAlmacenamientoService almacenamientoService,
            IIntegridadService integridadService,
            IAuditoriaService auditoriaService,
            IHistorialActividadService historialActividadService,
            ILogger<DocumentoService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _documentoRepo = documentoRepo;
            _versionRepo = versionRepo;
            _relacionRepo = relacionRepo;
            _actividadRepo = actividadRepo;
            _dominioRepo = dominioRepo;
            _procesoRepo = procesoRepo;
            _subdominioRepo = subdominioRepo;
            _almacenamientoService = almacenamientoService;
            _integridadService = integridadService;
            _auditoriaService = auditoriaService;
            _historialActividadService = historialActividadService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // ──────────────────────────────────────────────
        //  Listado y detalle
        // ──────────────────────────────────────────────

        public async Task<IEnumerable<object>> ObtenerDocumentosActividadAsync(int actividadId)
        {
            var actividad = await _actividadRepo.ObtenerPorId(actividadId);
            if (actividad == null)
                throw new KeyNotFoundException($"Actividad {actividadId} no encontrada");

            var documentos = await _documentoRepo.ObtenerPorActividadId(actividadId);
            return documentos.Select(d => MapDocumentoListaToObject(d));
        }

        public async Task<object?> ObtenerDocumentoAsync(int id)
        {
            var documento = await _documentoRepo.ObtenerConVersiones(id);
            if (documento == null) return null;

            // Cargar relaciones por separado para evitar query demasiado compleja
            var relaciones = await _relacionRepo.ObtenerTodasPorDocumentoId(id);

            return MapDocumentoDetalleToObject(documento, relaciones);
        }

        // ──────────────────────────────────────────────
        //  Crear documento (flujo transaccional de 4 pasos)
        // ──────────────────────────────────────────────

        public async Task<object> CrearDocumentoAsync(DocumentoCreateDto dto, int usuarioId)
        {
            // Validaciones de entrada
            if (dto.TipoDocumento == "URL" && string.IsNullOrWhiteSpace(dto.Url))
                throw new ArgumentException("Para TipoDocumento 'URL' debe proporcionar una URL");

            if (dto.TipoDocumento != "URL" && dto.Archivo == null)
                throw new ArgumentException("Debe proporcionar un archivo o usar TipoDocumento 'URL'");

            var actividad = await _actividadRepo.ObtenerPorId(dto.ActividadId);
            if (actividad == null)
                throw new KeyNotFoundException($"Actividad {dto.ActividadId} no encontrada");

            await ValidarActividadImplementableParaEdicionAsync(dto.ActividadId);

            // Validar: solo puede existir un documento Principal por actividad
            var rolEfectivo = (dto.RolEnActividad ?? "Anexo").Trim();
            if (rolEfectivo == "Principal")
            {
                var existePrincipal = await _documentoRepo.ExistePrincipalEnActividadAsync(dto.ActividadId);
                if (existePrincipal)
                    throw new InvalidOperationException(
                        "Ya existe un documento principal para esta actividad. Elimine el actual o créelo como Anexo.");
            }

            string? rutaTemporal = null;
            string? checksum = null;
            // Para documentos URL: si la descarga tiene éxito, se guarda como archivo.
            // Si falla (requiere login, CORS, timeout, etc.) se guarda solo el enlace.
            IFormFile? archivoDescargado = null;
            bool descargaExitosa = false;

            try
            {
                // PASO 1a: Subida de archivo normal
                if (dto.Archivo != null)
                {
                    var (valido, error) = await _almacenamientoService.ValidarArchivoAsync(dto.Archivo);
                    if (!valido) throw new ArgumentException(error);

                    rutaTemporal = await _almacenamientoService.GuardarTemporalAsync(dto.Archivo);

                    using (var fs = File.OpenRead(rutaTemporal))
                        checksum = await _integridadService.CalcularChecksumAsync(fs);
                }
                // PASO 1b: Si es URL, intentar descargar el archivo
                else if (dto.TipoDocumento == "URL" && !string.IsNullOrWhiteSpace(dto.Url))
                {
                    try
                    {
                        var httpClient = _httpClientFactory.CreateClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("User-Agent",
                            "Mozilla/5.0 (compatible; DocumentoService/1.0)");

                        using var response = await httpClient.GetAsync(dto.Url,
                            HttpCompletionOption.ResponseHeadersRead);

                        if (response.IsSuccessStatusCode)
                        {
                            // Determinar nombre y extensión del archivo
                            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                            var disposicion = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
                            var extension = ObtenerExtensionDesdeContentType(contentType);
                            var nombreArchivo = !string.IsNullOrWhiteSpace(disposicion)
                                ? disposicion
                                : $"{SanitizarNombreArchivo(dto.Nombre)}{extension}";

                            // Solo descargar tipos de archivo aceptados (no páginas HTML)
                            var extensionesAceptadas = new[] { ".pdf", ".docx", ".doc", ".pptx", ".xlsx", ".xls" };
                            if (extensionesAceptadas.Contains(extension, StringComparer.OrdinalIgnoreCase))
                            {
                                var contenido = await response.Content.ReadAsByteArrayAsync();
                                // Límite: 50 MB
                                if (contenido.Length <= 50 * 1024 * 1024)
                                {
                                    archivoDescargado = new DescargaFormFile(contenido, nombreArchivo, contentType);
                                    var (valido, error) = await _almacenamientoService.ValidarArchivoAsync(archivoDescargado);
                                    if (valido)
                                    {
                                        rutaTemporal = await _almacenamientoService.GuardarTemporalAsync(archivoDescargado);
                                        using (var fs = File.OpenRead(rutaTemporal))
                                            checksum = await _integridadService.CalcularChecksumAsync(fs);
                                        descargaExitosa = true;
                                        _logger.LogInformation(
                                            "URL descargada exitosamente: {Url} → {Nombre}", dto.Url, nombreArchivo);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Descarga fallida: continuar y guardar solo la URL
                        _logger.LogWarning("No se pudo descargar la URL {Url}: {Error}", dto.Url, ex.Message);
                    }
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // PASO 2: Crear documento con VersionActualId = null
                    var documento = new Documento
                    {
                        Nombre = dto.Nombre.Trim(),
                        Descripcion = dto.Descripcion?.Trim(),
                        TipoDocumento = dto.TipoDocumento,
                        ActividadId = dto.ActividadId,
                        VersionActualId = null,
                        Estado = "Borrador",
                        FechaVencimiento = dto.FechaVencimiento,
                        FechaAlerta = dto.FechaAlerta,
                        Categoria = dto.Categoria?.Trim(),
                        Confidencialidad = dto.Confidencialidad ?? "Interna",
                        RolEnActividad = rolEfectivo,
                        CreadoPorId = usuarioId,
                        FechaCreacion = DateTime.UtcNow,
                        Eliminado = false
                    };

                    await _documentoRepo.Agregar(documento);
                    await _documentoRepo.GuardarCambios(); // necesario para obtener IdDocumento

                    // PASO 3: Mover archivo a repositorio y crear versión 1
                    // El archivo puede venir de subida directa o de descarga exitosa desde URL.
                    string? rutaFinal = null;
                    var archivoEfectivo = dto.Archivo ?? (descargaExitosa ? archivoDescargado : null);
                    if (archivoEfectivo != null && rutaTemporal != null)
                    {
                        rutaFinal = await _almacenamientoService.MoverARepositorioAsync(
                            rutaTemporal,
                            dto.ActividadId,
                            documento.IdDocumento,
                            1,
                            archivoEfectivo.FileName);

                        rutaTemporal = null; // archivo movido → no limpiar temp
                    }

                    // TipoAlmacenamiento: "Archivo" si hay ruta física, "URL" si solo enlace
                    var tipoAlmacenamiento = rutaFinal != null ? "Archivo" : "URL";

                    var version = new VersionDocumento
                    {
                        DocumentoId = documento.IdDocumento,
                        NumeroVersion = 1,
                        VersionTexto = "1.0",
                        TipoAlmacenamiento = tipoAlmacenamiento,
                        RutaArchivo = rutaFinal,
                        // Siempre conservar la URL original como referencia
                        Url = dto.TipoDocumento == "URL" ? dto.Url : null,
                        NombreArchivoOriginal = archivoEfectivo?.FileName,
                        TamanoBytes = archivoEfectivo?.Length,
                        MimeType = archivoEfectivo?.ContentType,
                        ChecksumSHA256 = checksum,
                        Comentario = dto.ComentarioVersion,
                        SubidoPorId = usuarioId,
                        FechaSubida = DateTime.UtcNow,
                        Activo = true
                    };

                    await _versionRepo.Agregar(version);
                    await _versionRepo.GuardarCambios(); // necesario para obtener IdVersionDocumento

                    // PASO 4: Apuntar el documento a su primera versión (cierra la circular)
                    documento.VersionActualId = version.IdVersionDocumento;
                    await _documentoRepo.Actualizar(documento);
                    await _documentoRepo.GuardarCambios();

                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Documento {Id} '{Nombre}' creado por usuario {UsrId} en actividad {ActId}",
                        documento.IdDocumento, documento.Nombre, usuarioId, dto.ActividadId);

                    await _auditoriaService.RegistrarEventoAsync(
                        "Creación",
                        $"Documento {documento.IdDocumento} '{documento.Nombre}' creado en actividad {dto.ActividadId}",
                        "Documentos", usuarioId);

                    await _historialActividadService.RegistrarVersionAnteriorAsync(
                        dto.ActividadId,
                        "Documentos: se agrego un documento",
                        usuarioId);

                    return MapDocumentoListaToObject(documento, version);
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

        // ──────────────────────────────────────────────
        //  Eliminar (soft delete)
        // ──────────────────────────────────────────────

        public async Task<string> EliminarDocumentoAsync(int id, int usuarioId)
        {
            var documento = await _documentoRepo.ObtenerConVersionActual(id);
            if (documento == null)
                throw new KeyNotFoundException($"Documento {id} no encontrado");

            await ValidarActividadImplementableParaEdicionAsync(documento.ActividadId);

            if (documento.Eliminado)
                return "El documento ya estaba eliminado";

            // Verificar que no tenga relaciones activas como destino
            var relacionesComoDestino = await _relacionRepo.ObtenerPorDocumentoDestinoId(id);
            if (relacionesComoDestino.Any())
                throw new InvalidOperationException(
                    "No se puede eliminar el documento porque otros documentos lo referencian activamente");

            // Registrar snapshot previo para conservar el documento en versiones históricas.
            await _historialActividadService.RegistrarVersionAnteriorAsync(
                documento.ActividadId,
                "Documentos: se elimino un documento",
                usuarioId);

            var resultado = await _documentoRepo.EliminarLogico(id, usuarioId);
            if (!resultado)
                throw new InvalidOperationException("No se pudo eliminar el documento");

            await _documentoRepo.GuardarCambios();

            _logger.LogInformation("Documento {Id} eliminado (soft) por usuario {UsrId}", id, usuarioId);

            await _auditoriaService.RegistrarEventoAsync(
                "Eliminación",
                $"Documento {id} eliminado",
                "Documentos", usuarioId);

            return "Documento eliminado correctamente";
        }

        // ──────────────────────────────────────────────        //  Actualizar metadatos
        // ────────────────────────────────────────────

        public async Task<object> ActualizarDocumentoAsync(int id, ActualizarDocumentoDto dto, int usuarioId)
        {
            var documento = await _documentoRepo.ObtenerConVersionActual(id);
            if (documento == null)
                throw new KeyNotFoundException($"Documento {id} no encontrado");

            await ValidarActividadImplementableParaEdicionAsync(documento.ActividadId);

            if (documento.Eliminado)
                throw new InvalidOperationException("El documento está eliminado");

            // Guardar valores anteriores para auditoría
            var anterior = new
            {
                documento.Nombre,
                documento.Descripcion,
                documento.Categoria,
                documento.FechaVencimiento,
                documento.FechaAlerta,
                documento.Confidencialidad
            };

            documento.Nombre = dto.Nombre.Trim();
            documento.Descripcion = dto.Descripcion?.Trim();
            documento.Categoria = dto.Categoria?.Trim();
            documento.FechaVencimiento = dto.FechaVencimiento;
            documento.FechaAlerta = dto.FechaAlerta;
            documento.Confidencialidad = dto.Confidencialidad ?? documento.Confidencialidad;
            documento.ModificadoPorId = usuarioId;
            documento.FechaModificacion = DateTime.UtcNow;

            await _documentoRepo.Actualizar(documento);
            await _documentoRepo.GuardarCambios();

            _logger.LogInformation(
                "Documento {Id} '{Nombre}' actualizado por usuario {UsrId}",
                id, documento.Nombre, usuarioId);

            await _auditoriaService.RegistrarEventoAsync(
                "Modificación",
                $"Documento {id} '{documento.Nombre}' actualizado",
                "Documentos", usuarioId,
                datosAnteriores: anterior,
                datosNuevos: new
                {
                    documento.Nombre,
                    documento.Descripcion,
                    documento.Categoria,
                    documento.FechaVencimiento,
                    documento.FechaAlerta,
                    documento.Confidencialidad
                });

            await _historialActividadService.RegistrarVersionAnteriorAsync(
                documento.ActividadId,
                "Documentos: se actualizaron metadatos de un documento",
                usuarioId);

            return MapDocumentoListaToObject(documento);
        }

        // ────────────────────────────────────────────        //  Cambio de estado
        // ──────────────────────────────────────────────

        public async Task<string> CambiarEstadoAsync(int id, CambiarEstadoDocumentoDto dto, int usuarioId)
        {
            var documento = await _documentoRepo.ObtenerConVersionActual(id);
            if (documento == null)
                throw new KeyNotFoundException($"Documento {id} no encontrado");

            await ValidarActividadImplementableParaEdicionAsync(documento.ActividadId);

            if (documento.Eliminado)
                throw new InvalidOperationException("El documento está eliminado");

            var estadoActual = documento.Estado;
            var estadoNuevo = dto.Estado;

            // Validar transición
            if (!TransicionesValidas.TryGetValue(estadoActual, out var estadosPermitidos)
                || !estadosPermitidos.Contains(estadoNuevo))
                throw new InvalidOperationException(
                    $"Transición de estado '{estadoActual}' → '{estadoNuevo}' no está permitida");

            documento.Estado = estadoNuevo;
            documento.ModificadoPorId = usuarioId;
            documento.FechaModificacion = DateTime.UtcNow;
            await _documentoRepo.Actualizar(documento);
            await _documentoRepo.GuardarCambios();

            _logger.LogInformation(
                "Documento {Id}: estado cambiado de '{De}' a '{A}' por usuario {UsrId}",
                id, estadoActual, estadoNuevo, usuarioId);

            await _auditoriaService.RegistrarEventoAsync(
                "CambioEstado",
                $"Documento {id} '{documento.Nombre}': '{estadoActual}' → '{estadoNuevo}'",
                "Documentos", usuarioId);

            return $"Estado actualizado: '{estadoActual}' → '{estadoNuevo}'";
        }

        // ──────────────────────────────────────────────
        //  Relaciones
        // ──────────────────────────────────────────────

        public async Task<object> CrearRelacionAsync(int documentoOrigenId, CrearRelacionDto dto, int usuarioId)
        {
            // Validar que ambos documentos existan
            var origen = await _documentoRepo.ObtenerConVersionActual(documentoOrigenId);
            if (origen == null || origen.Eliminado)
                throw new KeyNotFoundException($"Documento origen {documentoOrigenId} no encontrado");

            await ValidarActividadImplementableParaEdicionAsync(origen.ActividadId);

            var destino = await _documentoRepo.ObtenerConVersionActual(dto.DocumentoDestinoId);
            if (destino == null || destino.Eliminado)
                throw new KeyNotFoundException($"Documento destino {dto.DocumentoDestinoId} no encontrado");

            // Evitar auto-referencia
            if (documentoOrigenId == dto.DocumentoDestinoId)
                throw new ArgumentException("Un documento no puede relacionarse consigo mismo");

            // Verificar si la relación ya existe
            var existe = await _relacionRepo.ExisteRelacion(
                documentoOrigenId, dto.DocumentoDestinoId, dto.TipoRelacion);
            if (existe)
                throw new InvalidOperationException(
                    $"Ya existe una relación de tipo '{dto.TipoRelacion}' entre estos documentos");

            var relacion = new RelacionDocumento
            {
                DocumentoOrigenId = documentoOrigenId,
                DocumentoDestinoId = dto.DocumentoDestinoId,
                TipoRelacion = dto.TipoRelacion,
                Descripcion = dto.Descripcion,
                Orden = dto.Orden,
                CreadoPorId = usuarioId,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            await _relacionRepo.Agregar(relacion);
            await _relacionRepo.GuardarCambios();

            _logger.LogInformation(
                "Relación '{Tipo}' creada: documento {Origen} → {Destino} por usuario {UsrId}",
                dto.TipoRelacion, documentoOrigenId, dto.DocumentoDestinoId, usuarioId);

            return MapRelacionToObject(relacion, origen.Nombre, destino.Nombre);
        }

        public async Task<IEnumerable<object>> ObtenerRelacionesAsync(int documentoId)
        {
            var documento = await _documentoRepo.ObtenerConVersionActual(documentoId);
            if (documento == null)
                throw new KeyNotFoundException($"Documento {documentoId} no encontrado");

            var relaciones = await _relacionRepo.ObtenerTodasPorDocumentoId(documentoId);
            return relaciones.Select(r => MapRelacionToObject(
                r,
                r.DocumentoOrigen?.Nombre ?? string.Empty,
                r.DocumentoDestino?.Nombre ?? string.Empty));
        }

        public async Task<string> EliminarRelacionAsync(int relacionId, int usuarioId)
        {
            var relacion = await _relacionRepo.ObtenerPorId(relacionId);
            if (relacion == null || !relacion.Activo)
                throw new KeyNotFoundException($"Relación {relacionId} no encontrada");

            var documentoOrigen = await _documentoRepo.ObtenerConVersionActual(relacion.DocumentoOrigenId);
            if (documentoOrigen == null)
                throw new KeyNotFoundException($"Documento origen {relacion.DocumentoOrigenId} no encontrado");

            await ValidarActividadImplementableParaEdicionAsync(documentoOrigen.ActividadId);

            var resultado = await _relacionRepo.DesactivarRelacion(relacionId);
            if (!resultado)
                throw new InvalidOperationException("No se pudo eliminar la relación");

            await _relacionRepo.GuardarCambios();

            _logger.LogInformation(
                "Relación {Id} desactivada por usuario {UsrId}", relacionId, usuarioId);

            return "Relación eliminada correctamente";
        }

        private async Task ValidarActividadImplementableParaEdicionAsync(int actividadId)
        {
            var actividad = await _actividadRepo.ObtenerPorId(actividadId);
            if (actividad == null)
                throw new KeyNotFoundException($"Actividad {actividadId} no encontrada");

            if (string.Equals(actividad.Implementable, "No", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "La actividad esta marcada como no implementable. Los documentos solo pueden verse y descargarse.");
            }
        }

        // ──────────────────────────────────────────────
        //  Mapeo
        // ──────────────────────────────────────────────

        private static object MapDocumentoListaToObject(Documento d, VersionDocumento? version = null)
        {
            var v = version ?? d.VersionActual;
            var bloqueadoPorImplementable = string.Equals(
                d.Actividad?.Implementable,
                "No",
                StringComparison.OrdinalIgnoreCase);

            return new
            {
                id = d.IdDocumento,
                nombre = d.Nombre,
                descripcion = d.Descripcion,
                tipoDocumento = d.TipoDocumento,
                actividadId = d.ActividadId,
                actividadImplementable = d.Actividad?.Implementable,
                bloqueadoEdicionPorImplementable = bloqueadoPorImplementable,
                rolEnActividad = d.RolEnActividad,
                estado = d.Estado,
                fechaVencimiento = d.FechaVencimiento,
                fechaAlerta = d.FechaAlerta,
                categoria = d.Categoria,
                confidencialidad = d.Confidencialidad,
                creadoPorId = d.CreadoPorId,
                fechaCreacion = d.FechaCreacion,
                eliminado = d.Eliminado,
                versionActual = v == null ? null : new
                {
                    id = v.IdVersionDocumento,
                    numeroVersion = v.NumeroVersion,
                    versionTexto = v.VersionTexto,
                    tipoAlmacenamiento = v.TipoAlmacenamiento,
                    url = v.Url,
                    nombreArchivoOriginal = v.NombreArchivoOriginal,
                    tamanoBytes = v.TamanoBytes,
                    subidoPorNombre = v.SubidoPor?.nombre,
                    fechaSubida = v.FechaSubida
                }
            };
        }

        private static object MapDocumentoDetalleToObject(
            Documento d,
            IEnumerable<RelacionDocumento> relaciones) => new
            {
                bloqueadoEdicionPorImplementable = string.Equals(
                    d.Actividad?.Implementable,
                    "No",
                    StringComparison.OrdinalIgnoreCase),
                id = d.IdDocumento,
                nombre = d.Nombre,
                descripcion = d.Descripcion,
                tipoDocumento = d.TipoDocumento,
                actividadId = d.ActividadId,
                actividadImplementable = d.Actividad?.Implementable,
                rolEnActividad = d.RolEnActividad,
                estado = d.Estado,
                fechaVencimiento = d.FechaVencimiento,
                fechaAlerta = d.FechaAlerta,
                categoria = d.Categoria,
                confidencialidad = d.Confidencialidad,
                creadoPorId = d.CreadoPorId,
                fechaCreacion = d.FechaCreacion,
                modificadoPorId = d.ModificadoPorId,
                fechaModificacion = d.FechaModificacion,
                versiones = d.Versiones
                .OrderByDescending(v => v.NumeroVersion)
                .Select(v => new
                {
                    id = v.IdVersionDocumento,
                    numeroVersion = v.NumeroVersion,
                    versionTexto = v.VersionTexto,
                    esVersionActual = v.IdVersionDocumento == d.VersionActualId,
                    tipoAlmacenamiento = v.TipoAlmacenamiento,
                    url = v.Url,
                    nombreArchivoOriginal = v.NombreArchivoOriginal,
                    tamanoBytes = v.TamanoBytes,
                    mimeType = v.MimeType,
                    comentario = v.Comentario,
                    fechaVencimiento = v.FechaVencimiento,
                    subidoPorNombre = v.SubidoPor?.nombre,
                    fechaSubida = v.FechaSubida
                }),
                relaciones = relaciones.Select(r => new
                {
                    id = r.IdRelacionDocumento,
                    documentoOrigenId = r.DocumentoOrigenId,
                    documentoDestinoId = r.DocumentoDestinoId,
                    tipoRelacion = r.TipoRelacion,
                    descripcion = r.Descripcion,
                    orden = r.Orden,
                    documentoOrigenNombre = r.DocumentoOrigen?.Nombre,
                    documentoDestinoNombre = r.DocumentoDestino?.Nombre
                })
            };

        private static object MapRelacionToObject(
            RelacionDocumento r, string nombreOrigen, string nombreDestino) => new
            {
                id = r.IdRelacionDocumento,
                documentoOrigenId = r.DocumentoOrigenId,
                documentoDestinoId = r.DocumentoDestinoId,
                tipoRelacion = r.TipoRelacion,
                descripcion = r.Descripcion,
                orden = r.Orden,
                documentoOrigenNombre = nombreOrigen,
                documentoDestinoNombre = nombreDestino,
                creadoPorId = r.CreadoPorId,
                fechaCreacion = r.FechaCreacion,
                activo = r.Activo
            };

        // ──────────────────────────────────────────────
        //  Fase 5 – Búsqueda, Alertas y Estadísticas
        // ──────────────────────────────────────────────

        public async Task<IEnumerable<object>> BuscarDocumentosAsync(BuscarDocumentosDto filtros)
        {
            var documentos = (await _documentoRepo.BuscarConFiltrosAsync(filtros)).ToList();

            var actividadIds = documentos.Select(d => d.ActividadId).Distinct().ToList();
            var actividades = await _actividadRepo.BuscarAsync(a => actividadIds.Contains(a.IdActividad));
            var actividadesPorId = actividades.ToDictionary(a => a.IdActividad, a => a.Nombre);

            return documentos.Select(d =>
            {
                var v = d.VersionActual;
                actividadesPorId.TryGetValue(d.ActividadId, out var actividadNombre);

                return new
                {
                    id = d.IdDocumento,
                    nombre = d.Nombre,
                    descripcion = d.Descripcion,
                    tipoDocumento = d.TipoDocumento,
                    actividadId = d.ActividadId,
                    actividadNombre,
                    procesoCodigo = d.Actividad?.Subdominio?.Proceso?.Codigo,
                    responsableNombre = d.Actividad?.FuncionariosResponsables?.nombre,
                    rolEnActividad = d.RolEnActividad,
                    estado = d.Estado,
                    fechaVencimiento = d.FechaVencimiento,
                    fechaAlerta = d.FechaAlerta,
                    categoria = d.Categoria,
                    confidencialidad = d.Confidencialidad,
                    creadoPorId = d.CreadoPorId,
                    fechaCreacion = d.FechaCreacion,
                    eliminado = d.Eliminado,
                    versionActual = v == null ? null : new
                    {
                        id = v.IdVersionDocumento,
                        numeroVersion = v.NumeroVersion,
                        versionTexto = v.VersionTexto,
                        tipoAlmacenamiento = v.TipoAlmacenamiento,
                        url = v.Url,
                        nombreArchivoOriginal = v.NombreArchivoOriginal,
                        tamanoBytes = v.TamanoBytes,
                        subidoPorNombre = v.SubidoPor?.nombre,
                        fechaSubida = v.FechaSubida
                    }
                };
            });
        }

        public async Task<object> ObtenerAlertasVencimientoAsync(int dias = 30)
        {
            var hoy = DateTime.UtcNow.Date;

            var vencidos = (await _documentoRepo.ObtenerVencidosConJerarquiaAsync()).ToList();
            var proximosAVencer = (await _documentoRepo.ObtenerProximosAVencerConJerarquiaAsync(dias)).ToList();

            return new
            {
                totalVencidos = vencidos.Count,
                totalProximosAVencer = proximosAVencer.Count,
                diasConfiguracion = dias,
                vencidos = vencidos.Select(d => new
                {
                    id = d.IdDocumento,
                    nombre = d.Nombre,
                    estado = d.Estado,
                    actividadId = d.ActividadId,
                    actividadNombre = d.Actividad?.Nombre,
                    dominioNombre = d.Actividad?.Subdominio?.Proceso?.Dominio?.Nombre,
                    fechaVencimiento = d.FechaVencimiento,
                    diasVencido = (int)(hoy - d.FechaVencimiento!.Value.Date).TotalDays
                }),
                proximosAVencer = proximosAVencer.Select(d => new
                {
                    id = d.IdDocumento,
                    nombre = d.Nombre,
                    estado = d.Estado,
                    actividadId = d.ActividadId,
                    actividadNombre = d.Actividad?.Nombre,
                    dominioNombre = d.Actividad?.Subdominio?.Proceso?.Dominio?.Nombre,
                    fechaVencimiento = d.FechaVencimiento,
                    diasRestantes = (int)(d.FechaVencimiento!.Value.Date - hoy).TotalDays
                })
            };
        }

        // ── Transparencia pública ──────────────────────────────────────────────

        public async Task<object> ObtenerDocumentosPublicosAsync()
        {
            // Cargar jerarquía a través de los repositorios (sin acceso directo a _context)
            var dominios    = (await _dominioRepo.ObtenerTodos()).OrderBy(d => d.Nombre).ToList();
            var procesos    = (await _procesoRepo.ObtenerTodos()).OrderBy(p => p.Codigo).ToList();
            var subdominios = (await _subdominioRepo.ObtenerTodos()).OrderBy(s => s.IdSubdominio).ToList();
            var actividades = (await _actividadRepo.ObtenerTodos()).OrderBy(a => a.Nombre).ToList();
            var documentos  = (await _documentoRepo.ObtenerPublicosConVersionAsync()).ToList();

            var resultado = dominios.Select(dominio =>
            {
                var procesosDominio = procesos
                    .Where(p => p.DominioId == dominio.IdDominio)
                    .Select(proceso =>
                    {
                        var subdominiosProceso = subdominios
                            .Where(s => s.ProcesoId == proceso.IdProceso)
                            .Select(sub =>
                            {
                                var actividadesSub = actividades
                                    .Where(a => a.SubdominioId == sub.IdSubdominio)
                                    .Select(act =>
                                    {
                                        var docsActividad = documentos
                                            .Where(d => d.ActividadId == act.IdActividad)
                                            .Select(d => new
                                            {
                                                id = d.IdDocumento,
                                                nombre = d.Nombre,
                                                descripcion = d.Descripcion,
                                                tipo = d.TipoDocumento,
                                                estado = d.Estado,
                                                categoria = d.Categoria,
                                                rolEnActividad = d.RolEnActividad,
                                                fechaCreacion = d.FechaCreacion,
                                                fechaVencimiento = d.FechaVencimiento,
                                                version = d.VersionActual != null
                                                    ? $"v{d.VersionActual.NumeroVersion}"
                                                    : null,
                                                esUrl = d.TipoDocumento == "URL",
                                                urlDescarga = $"/api/transparencia/documentos/{d.IdDocumento}/descargar"
                                            })
                                            .ToList();

                                        return new
                                        {
                                            id = act.IdActividad,
                                            nombre = act.Nombre,
                                            porcentajeAvance = act.PorcentajeAvance,
                                            documentos = docsActividad,
                                            totalDocumentos = docsActividad.Count
                                        };
                                    })
                                    .Where(a => a.documentos.Count > 0)
                                    .ToList();

                                return new
                                {
                                    id = sub.IdSubdominio,
                                    practicasGobierno = sub.PracticasGobierno,
                                    indicadoresAsociados = sub.IndicadoresAsociados,
                                    actividades = actividadesSub,
                                    totalDocumentos = actividadesSub.Sum(a => a.totalDocumentos)
                                };
                            })
                            .Where(s => s.totalDocumentos > 0)
                            .ToList();

                        return new
                        {
                            id = proceso.IdProceso,
                            codigo = proceso.Codigo,
                            nombre = proceso.Nombre,
                            marcoNormativo = proceso.MarcoNormativo,
                            subdominios = subdominiosProceso,
                            totalDocumentos = subdominiosProceso.Sum(s => s.totalDocumentos)
                        };
                    })
                    .Where(p => p.totalDocumentos > 0)
                    .ToList();

                return new
                {
                    id = dominio.IdDominio,
                    nombre = dominio.Nombre,
                    procesos = procesosDominio,
                    totalDocumentos = procesosDominio.Sum(p => p.totalDocumentos)
                };
            })
            .Where(d => d.totalDocumentos > 0)
            .ToList();

            return new
            {
                totalDocumentos = documentos.Count,
                totalDominios = resultado.Count,
                dominios = resultado,
                ultimaActualizacion = DateTime.UtcNow
            };
        }

        public async Task<(byte[]? Bytes, string? ContentType, string? FileName, string? UrlExterna)>
            ResolverDescargaPublicaAsync(int id)
        {
            // Buscar el documento: solo requiere que sea Público y no eliminado.
            var documento = await _documentoRepo.ObtenerPublicoPorIdAsync(id);

            if (documento == null)
            {
                _logger.LogWarning(
                    "Intento de descarga pública rechazado — id={Id} no existe o no es público.", id);

                throw new KeyNotFoundException("Documento no encontrado o no disponible públicamente");
            }

            var version = documento.VersionActual;
            if (version == null)
                throw new KeyNotFoundException("El documento no tiene una versión disponible");

            // Documento de tipo URL → devolver la URL externa
            if (documento.TipoDocumento == "URL")
            {
                var urlExterna = version.Url ?? version.RutaArchivo;
                if (string.IsNullOrEmpty(urlExterna))
                    throw new KeyNotFoundException("El documento no tiene una URL configurada");

                return (null, null, null, urlExterna);
            }

            // Documento de tipo archivo → resolver ruta física y leer bytes
            if (string.IsNullOrEmpty(version.RutaArchivo))
                throw new KeyNotFoundException("El archivo no tiene una ruta configurada");

            // Intentar ruta relativa al repositorio primero, luego ruta absoluta directa
            string rutaAbsoluta;
            if (_almacenamientoService.ArchivoExiste(version.RutaArchivo))
                rutaAbsoluta = _almacenamientoService.ObtenerRutaCompleta(version.RutaArchivo);
            else if (System.IO.File.Exists(version.RutaArchivo))
                rutaAbsoluta = version.RutaArchivo;
            else
                throw new KeyNotFoundException("El archivo no se encuentra en el servidor");

            var bytes = await System.IO.File.ReadAllBytesAsync(rutaAbsoluta);

            var contentType = documento.TipoDocumento.ToUpper() switch
            {
                "PDF" => "application/pdf",
                "DOCX" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "DOC" => "application/msword",
                "PPTX" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "XLSX" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "XLS" => "application/vnd.ms-excel",
                _ => "application/octet-stream"
            };

            var nombreArchivo = version.NombreArchivoOriginal
                ?? $"{documento.Nombre}.{documento.TipoDocumento.ToLower()}";

            return (bytes, contentType, nombreArchivo, null);
        }

        public async Task<object> ObtenerEstadisticasDocumentosAsync()
        {
            var todos = (await _documentoRepo.ObtenerTodosNoEliminadosAsync()).ToList();

            var hoy = DateTime.UtcNow.Date;

            var porEstado = todos
                .GroupBy(d => d.Estado)
                .Select(g => new { estado = g.Key, cantidad = g.Count() })
                .OrderByDescending(x => x.cantidad);

            var porTipo = todos
                .GroupBy(d => d.TipoDocumento)
                .Select(g => new { tipo = g.Key, cantidad = g.Count() })
                .OrderByDescending(x => x.cantidad);

            var vencidos = todos.Count(d =>
                d.FechaVencimiento.HasValue
                && d.FechaVencimiento.Value.Date < hoy
                && d.Estado != "Obsoleto"
                && d.Estado != "Archivado");

            var vencen30 = todos.Count(d =>
                d.FechaVencimiento.HasValue
                && d.FechaVencimiento.Value.Date >= hoy
                && d.FechaVencimiento.Value.Date <= hoy.AddDays(30)
                && d.Estado != "Obsoleto"
                && d.Estado != "Archivado");

            var totalVersiones = await _versionRepo.ContarVersionesActivasAsync();

            return new
            {
                total = todos.Count,
                totalVersiones,
                vencidos,
                vencen30Dias = vencen30,
                porEstado,
                porTipo,
                fechaConsulta = DateTime.UtcNow
            };
        }

        // ──────────────────────────────────────────────
        //  Helpers para descarga desde URL
        // ──────────────────────────────────────────────

        private static string ObtenerExtensionDesdeContentType(string contentType) => contentType switch
        {
            "application/pdf"                                                          => ".pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/msword"                                                       => ".doc",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"       => ".xlsx",
            "application/vnd.ms-excel"                                                 => ".xls",
            _ => ""
        };

        private static string SanitizarNombreArchivo(string nombre)
        {
            var invalidos = Path.GetInvalidFileNameChars();
            return string.Concat(nombre.Select(c => invalidos.Contains(c) ? '_' : c));
        }
    }

    /// <summary>
    /// Adapta bytes descargados desde una URL al contrato IFormFile
    /// que espera AlmacenamientoService, sin necesidad de escribir a disco.
    /// </summary>
    internal sealed class DescargaFormFile : IFormFile
    {
        private readonly byte[] _contenido;
        public string ContentType { get; }
        public string ContentDisposition => $"attachment; filename=\"{FileName}\"";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length => _contenido.Length;
        public string Name => "archivo";
        public string FileName { get; }

        public DescargaFormFile(byte[] contenido, string fileName, string contentType)
        {
            _contenido = contenido;
            FileName = fileName;
            ContentType = contentType;
        }

        public void CopyTo(Stream target) => target.Write(_contenido, 0, _contenido.Length);
        public async Task CopyToAsync(Stream target, CancellationToken ct = default)
            => await target.WriteAsync(_contenido, 0, _contenido.Length, ct);
        public Stream OpenReadStream() => new MemoryStream(_contenido);
    }
}