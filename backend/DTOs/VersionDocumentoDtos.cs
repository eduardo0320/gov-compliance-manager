using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace backend.DTOs;

/// <summary>Sube una nueva versión a un documento existente.</summary>
public class SubirVersionDto
{
    // Archivo físico (para TipoAlmacenamiento = Archivo)
    public IFormFile? Archivo { get; set; }

    // URL externa (para TipoAlmacenamiento = URL)
    [MaxLength(2000)]
    public string? Url { get; set; }

    /// <summary>Descripción de los cambios incluidos en esta versión.</summary>
    public string? Comentario { get; set; }

    /// <summary>Fecha de vencimiento de esta versión.</summary>
    [Required]
    public DateTime? FechaVencimiento { get; set; }

    /// <summary>menor (default): 1.0 → 1.1  |  mayor: 1.0 → 2.0</summary>
    [MaxLength(5)]
    public string? TipoVersionamiento { get; set; }
}
