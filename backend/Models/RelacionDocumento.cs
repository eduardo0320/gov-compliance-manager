using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("relaciones_documento")]
public class RelacionDocumento
{
    [Key, Column("id_RelacionDocumento")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdRelacionDocumento { get; set; }

    // Dos FK al mismo tipo (Documento) — configuradas vía Fluent API en NormasDb
    [Required]
    [Column("DocumentoOrigenId")]
    public int DocumentoOrigenId { get; set; }

    public Documento DocumentoOrigen { get; set; } = null!;

    [Required]
    [Column("DocumentoDestinoId")]
    public int DocumentoDestinoId { get; set; }

    public Documento DocumentoDestino { get; set; } = null!;

    // En BD es ENUM('Anexo','Referencia','Dependencia','Reemplaza','Relacionado'); aquí como string
    [Required, MaxLength(20)]
    [Column("TipoRelacion")]
    public string TipoRelacion { get; set; } = null!;

    [MaxLength(500)]
    [Column("Descripcion")]
    public string? Descripcion { get; set; }

    // Permite ordenar anexos: "Anexo A" = 1, "Anexo B" = 2, etc.
    [Column("Orden")]
    public int? Orden { get; set; }

    [Required]
    [Column("CreadoPorId")]
    public int CreadoPorId { get; set; }

    [ForeignKey(nameof(CreadoPorId))]
    public Usuario CreadoPor { get; set; } = null!;

    [Required]
    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("Activo")]
    public bool Activo { get; set; } = true;
}
