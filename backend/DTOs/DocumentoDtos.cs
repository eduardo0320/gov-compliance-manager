using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace backend.DTOs;

// ──────────────────────────────────────────────
//  Entrada
// ──────────────────────────────────────────────

/// <summary>Crea un documento nuevo junto con su primera versión (v1.0).</summary>
public class DocumentoCreateDto
{
    [Required, MaxLength(255)]
    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    /// <summary>PDF | DOCX | DOC | PPTX | XLSX | XLS | URL | OTRO</summary>
    [Required, MaxLength(10)]
    public string TipoDocumento { get; set; } = null!;

    [Required]
    public int ActividadId { get; set; }

    public DateTime? FechaVencimiento { get; set; }
    public DateTime? FechaAlerta { get; set; }

    [MaxLength(100)]
    public string? Categoria { get; set; }

    /// <summary>Publica | Interna | Confidencial | Restringida — Default: Interna</summary>
    [MaxLength(20)]
    public string? Confidencialidad { get; set; }

    /// <summary>Principal | Anexo — Default: Anexo. Solo puede existir un documento Principal por actividad.</summary>
    [MaxLength(20)]
    public string? RolEnActividad { get; set; }

    // Archivo físico (cuando TipoDocumento != URL)
    public IFormFile? Archivo { get; set; }

    // URL externa (cuando TipoDocumento == URL)
    [MaxLength(2000)]
    public string? Url { get; set; }

    /// <summary>Comentario de la primera versión (opcional).</summary>
    public string? ComentarioVersion { get; set; }
}

/// <summary>Filtros opcionales para buscar documentos.</summary>
public class BuscarDocumentosDto
{
    /// <summary>Búsqueda parcial por nombre (LIKE %texto%).</summary>
    public string? Nombre { get; set; }

    /// <summary>Filtrar por estado exacto.</summary>
    public string? Estado { get; set; }

    /// <summary>Filtrar por tipo de documento.</summary>
    public string? TipoDocumento { get; set; }

    /// <summary>Filtrar por actividad específica.</summary>
    public int? ActividadId { get; set; }

    /// <summary>Fecha de vencimiento desde (inclusive).</summary>
    public DateTime? VencimientoDesde { get; set; }

    /// <summary>Fecha de vencimiento hasta (inclusive).</summary>
    public DateTime? VencimientoHasta { get; set; }

    /// <summary>Solo documentos vencidos (FechaVencimiento &lt; hoy).</summary>
    public bool? SoloVencidos { get; set; }

    /// <summary>Código del proceso asociado a la actividad.</summary>
    public string? CodigoProceso { get; set; }

    /// <summary>Número máximo de resultados (default 50).</summary>
    public int Limite { get; set; } = 50;
}

/// <summary>Cambia el estado de un documento.</summary>
public class CambiarEstadoDocumentoDto
{
    /// <summary>Borrador | En_Revision | Aprobado | Vigente | Obsoleto | Archivado</summary>
    [Required, MaxLength(20)]
    public string Estado { get; set; } = null!;

    public string? Comentario { get; set; }
}

/// <summary>Actualiza los metadatos editables de un documento existente (no cambia tipo, actividad, estado ni rol).</summary>
public class ActualizarDocumentoDto
{
    [Required, MaxLength(255)]
    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    [MaxLength(100)]
    public string? Categoria { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public DateTime? FechaAlerta { get; set; }

    /// <summary>Publica | Interna | Confidencial | Restringida</summary>
    [MaxLength(20)]
    public string? Confidencialidad { get; set; }
}
