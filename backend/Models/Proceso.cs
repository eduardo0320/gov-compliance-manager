using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend.Models;

namespace backend.Models;

[Table("Proceso")]
public class Proceso
{
    [Key, Column("id_Proceso")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdProceso { get; set; }

    [Required, MaxLength(20)]
    [Column("Codigo")]
    public string Codigo { get; set; } = null!;

    [Required, MaxLength(500)]
    [Column("Nombre")]
    public string Nombre { get; set; } = null!;

    [Required, MaxLength(500)]
    [Column("MarcoNormativo")]
    public string MarcoNormativo { get; set; } = null!;

    // En BD es ENUM('Sí','No'); aquí como string
    [Required, MaxLength(12)]
    [Column("EstadoImplementacion")]
    public string EstadoImplementacion { get; set; } = "Sí";

    [Column("PorcentajeAvance", TypeName = "decimal(5,2)")]
    public decimal PorcentajeAvance { get; set; } = 0.00m;

    [Column("PrioridadImplementacion")]
    public int PrioridadImplementacion { get; set; } = 0;

    [Column("FechaConclusionImplementacion")]
    public DateTime? FechaConclusionImplementacion { get; set; }

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("FechaModificacion")]
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    [Column("CreadoPorId")]
    public int CreadoPorId { get; set; }

    [ForeignKey(nameof(CreadoPorId))]
    public Usuario? CreadoPor { get; set; }

    [Column("ModificadoPorId")]
    public int ModificadoPorId { get; set; }

    [ForeignKey(nameof(ModificadoPorId))]
    public Usuario? ModificadoPor { get; set; }

    [Required]
    [Column("DominioId")]
    public int DominioId { get; set; }

    [ForeignKey(nameof(DominioId))]
    public Dominio Dominio { get; set; } = null!;

    // Proceso → Subdominios (1:N)
    public ICollection<Subdominio> Subdominios { get; set; } = new List<Subdominio>();
}
