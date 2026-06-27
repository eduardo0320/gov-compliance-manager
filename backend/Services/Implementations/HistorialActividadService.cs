using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace backend.Services.Implementations;

public class HistorialActividadService : IHistorialActividadService
{
    private readonly NormasDb _db;
    private readonly ILogger<HistorialActividadService> _logger;

    public HistorialActividadService(NormasDb db, ILogger<HistorialActividadService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RegistrarVersionAnteriorAsync(int actividadId, string descripcionCambios, int? usuarioModificacionId = null)
    {
        var actividad = await _db.Actividades
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdActividad == actividadId);

        if (actividad == null)
            throw new KeyNotFoundException("Actividad no encontrada");

        var versionActual = await _db.HistorialVersionesActividades
            .Where(h => h.ActividadId == actividadId)
            .Select(h => (int?)h.Version)
            .MaxAsync() ?? 0;

        var documentosConClasificacion = await ObtenerDocumentosConClasificacionAsync(actividadId);
        var documentosAnteriores = await ConstruirDocumentosAnterioresAsync(actividadId);

        var historial = new HistorialVersionActividad
        {
            ActividadId = actividad.IdActividad,
            Version = versionActual + 1,
            FechaRegistro = DateTime.UtcNow,
            UsuarioModificacionId = usuarioModificacionId,
            Nombre = actividad.Nombre,
            Implementable = actividad.Implementable,
            FechaCompromiso = actividad.FechaCompromiso,
            EstadoImplementacion = actividad.EstadoImplementacion,
            PorcentajeAvance = actividad.PorcentajeAvance,
            FuncionariosResponsablesId = actividad.FuncionariosResponsablesId ?? 0,
            FechaControl = actividad.FechaControl,
            Documentos = documentosConClasificacion, // Ahora incluye clasificación en JSON
            DocumentosAnteriores = documentosAnteriores,
            Observaciones = actividad.Observaciones,
            DescripcionCambios = descripcionCambios
        };

        _db.HistorialVersionesActividades.Add(historial);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Historial de actividad registrado. ActividadId: {ActividadId}, Version: {Version}, Usuario: {UsuarioId}",
            actividadId,
            historial.Version,
            usuarioModificacionId);
    }

    public async Task<IEnumerable<object>> ObtenerHistorialPorActividadAsync(int actividadId)
    {
        var historial = await _db.HistorialVersionesActividades
            .AsNoTracking()
            .Where(h => h.ActividadId == actividadId)
            .OrderByDescending(h => h.Version)
            .ToListAsync();

        var usuariosIds = historial
            .Where(h => h.UsuarioModificacionId.HasValue)
            .Select(h => h.UsuarioModificacionId!.Value)
            .Distinct()
            .ToList();

        var usuariosPorId = usuariosIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Usuarios
                .AsNoTracking()
                .Where(u => usuariosIds.Contains(u.Id_Usuario))
                .Select(u => new { u.Id_Usuario, u.nombre })
                .ToDictionaryAsync(u => u.Id_Usuario, u => u.nombre);

        var resultado = historial.Select(h =>
        {
            var documentos = ParsearDocumentos(h.Documentos);
            var documentosAnteriores = ParsearDocumentos(h.DocumentosAnteriores);
            usuariosPorId.TryGetValue(h.UsuarioModificacionId ?? 0, out var usuarioNombre);

            return new
            {
                idHistorialActividad = h.IdHistorialActividad,
                version = h.Version,
                fechaRegistro = h.FechaRegistro,
                descripcionCambios = h.DescripcionCambios,
                usuarioModificacionId = h.UsuarioModificacionId,
                usuarioModificacionNombre = usuarioNombre,
                datosAnteriores = new
                {
                    nombre = h.Nombre,
                    implementable = h.Implementable,
                    fechaCompromiso = h.FechaCompromiso,
                    estadoImplementacion = h.EstadoImplementacion,
                    porcentajeAvance = h.PorcentajeAvance,
                    funcionariosResponsablesId = h.FuncionariosResponsablesId,
                    fechaControl = h.FechaControl,
                    documentos = documentos,
                    observaciones = h.Observaciones
                },
                documentosAnteriores = documentosAnteriores
            };
        }).ToList();

        return resultado;
    }

    private static List<Dictionary<string, object>>? ParsearDocumentos(string? documentosJson)
    {
        if (string.IsNullOrWhiteSpace(documentosJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(documentosJson);
        }
        catch
        {
            // Compatibilidad con formato antiguo (texto plano)
            return SepararDocumentos(documentosJson)
                .Select(d => new Dictionary<string, object> { { "id", 0 }, { "nombre", d }, { "rol", "Anexo" } })
                .ToList();
        }
    }

    private static IEnumerable<string> SepararDocumentos(string? documentos)
    {
        if (string.IsNullOrWhiteSpace(documentos))
            return Enumerable.Empty<string>();

        return documentos
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Where(d => !string.IsNullOrWhiteSpace(d));
    }

    private async Task<string?> ConstruirDocumentosAnterioresAsync(int actividadId)
    {
        var historialesPrevios = await _db.HistorialVersionesActividades
            .AsNoTracking()
            .Where(h => h.ActividadId == actividadId)
            .OrderBy(h => h.Version)
            .Select(h => h.Documentos)
            .ToListAsync();

            var documentosList = new List<Dictionary<string, object>>();

        foreach (var docJson in historialesPrevios)
        {
            if (string.IsNullOrWhiteSpace(docJson))
                continue;

            try
            {
                var docs = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(docJson);
                if (docs != null)
                    documentosList.AddRange(docs);
            }
            catch
            {
                // Si no es JSON válido, parsear como texto plano (compatibilidad hacia atrás)
                var docs = SepararDocumentos(docJson)
                    .Select(d => new Dictionary<string, object> { { "id", (object)0 }, { "nombre", (object)d }, { "rol", (object)"Anexo" } })
                    .ToList();
                documentosList.AddRange(docs);
            }
        }

        if (documentosList.Count == 0)
            return null;

        // Eliminar duplicados por nombre
        var documentosUnicos = documentosList
                .GroupBy(d => d["nombre"]?.ToString() ?? "")
            .Select(g => g.First())
            .ToList();

        return JsonSerializer.Serialize(documentosUnicos);
    }

    private async Task<string?> ObtenerDocumentosConClasificacionAsync(int actividadId)
    {
        var documentos = await _db.Documentos
            .AsNoTracking()
            .Include(d => d.VersionActual)
            .Where(d => d.ActividadId == actividadId && !d.Eliminado)
            .OrderByDescending(d => d.FechaCreacion)
            .Select(d => new
            {
                id = d.IdDocumento,
                nombre = d.VersionActual != null && !string.IsNullOrWhiteSpace(d.VersionActual.NombreArchivoOriginal)
                    ? d.VersionActual.NombreArchivoOriginal
                    : d.Nombre,
                rol = d.RolEnActividad
            })
            .ToListAsync();

        if (documentos.Count == 0)
            return null;

        return JsonSerializer.Serialize(documentos);
    }
}