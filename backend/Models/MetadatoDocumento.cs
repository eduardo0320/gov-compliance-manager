using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

// Tabla EAV (Entity-Attribute-Value) para metadatos opcionales o variables por tipo de documento.
// ADVERTENCIA: No usar para campos que aplican a la mayoría de los documentos —
// esos deben ser columnas reales en la tabla `documentos`.
[Table("metadatos_documento")]
public class MetadatoDocumento
{
    [Key, Column("id_MetadatoDocumento")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdMetadatoDocumento { get; set; }

    [Required]
    [Column("DocumentoId")]
    public int DocumentoId { get; set; }

    [ForeignKey(nameof(DocumentoId))]
    public Documento Documento { get; set; } = null!;

    // Clave del metadato: ej. "autor", "departamento_responsable"
    [Required, MaxLength(100)]
    [Column("Clave")]
    public string Clave { get; set; } = null!;

    [Column("Valor")]
    public string? Valor { get; set; }

    [Required]
    [Column("CreadoPorId")]
    public int CreadoPorId { get; set; }

    [ForeignKey(nameof(CreadoPorId))]
    public Usuario CreadoPor { get; set; } = null!;

    [Required]
    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
