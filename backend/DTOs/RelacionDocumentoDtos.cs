using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

/// <summary>Crea una relación entre dos documentos.</summary>
public class CrearRelacionDto
{
    [Required]
    public int DocumentoDestinoId { get; set; }

    /// <summary>Anexo | Referencia | Dependencia | Reemplaza | Relacionado</summary>
    [Required, MaxLength(20)]
    public string TipoRelacion { get; set; } = null!;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    /// <summary>Número de orden para anexos (Anexo A = 1, Anexo B = 2, etc.).</summary>
    public int? Orden { get; set; }
}
