using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend.Models;

namespace backend.Models;

[Table("Actividad")]
public class Actividad
{
    [Key, Column("id_Actividad")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdActividad { get; set; }

    [Required, MaxLength(500)]
    [Column("Nombre")]
    public string Nombre { get; set; } = null!;

    // En BD es ENUM('Sí','No'); aquí como string
    [Required, MaxLength(12)]
    [Column("Implementable")]
    public string Implementable { get; set; } = "Sí";

    [Column("FechaCompromiso")]
    public DateTime? FechaCompromiso { get; set; }

    // En BD es ENUM('Pendiente','En Progreso','En Revisión','Implementado'); aquí como string
    [Required, MaxLength(20)]
    [Column("EstadoImplementacion")]
    public string EstadoImplementacion { get; set; } = "Pendiente";

    [Column("PorcentajeAvance", TypeName = "decimal(5,2)")]
    public decimal PorcentajeAvance { get; set; } = 0.00m;

    [Column("FuncionariosResponsablesId")]
    public int? FuncionariosResponsablesId { get; set; }

    [ForeignKey(nameof(FuncionariosResponsablesId))]
    public Usuario? FuncionariosResponsables { get; set; }

    [Column("FechaControl")]
    public DateTime? FechaControl { get; set; }

    [Column("Documentos")]
    public string? Documentos { get; set; }

    [Column("Observaciones")]
    public string? Observaciones { get; set; }

    [Required]
    [Column("SubdominioId")]
    public int SubdominioId { get; set; }

    [ForeignKey(nameof(SubdominioId))]
    public Subdominio Subdominio { get; set; } = null!;

    // Actividad → Documentos (1:N)
    // Nombre distinto al campo legacy string 'Documentos' que se migrará en una fase posterior
    public ICollection<Documento> DocumentosVinculados { get; set; } = new List<Documento>();
}
