using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("HistorialVersionesActividades")]
public class HistorialVersionActividad
{
    [Key, Column("id_HistorialActividad")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdHistorialActividad { get; set; }

    [Required]
    [Column("ActividadId")]
    public int ActividadId { get; set; }

    [ForeignKey(nameof(ActividadId))]
    public Actividad Actividad { get; set; } = null!;

    [Required]
    [Column("Version")]
    public int Version { get; set; }

    [Required]
    [Column("FechaRegistro")]
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [Column("UsuarioModificacionId")]
    public int? UsuarioModificacionId { get; set; }

    [MaxLength(500)]
    [Column("Nombre")]
    public string Nombre { get; set; } = null!;

    [MaxLength(12)]
    [Column("Implementable")]
    public string Implementable { get; set; } = "Sí";

    [Column("FechaCompromiso")]
    public DateTime? FechaCompromiso { get; set; }

    [MaxLength(20)]
    [Column("EstadoImplementacion")]
    public string EstadoImplementacion { get; set; } = "Pendiente";

    [Column("PorcentajeAvance", TypeName = "decimal(5,2)")]
    public decimal PorcentajeAvance { get; set; }

    [Column("FuncionariosResponsablesId")]
    public int FuncionariosResponsablesId { get; set; }

    [Column("FechaControl")]
    public DateTime? FechaControl { get; set; }

    [Column("Documentos")]
    public string? Documentos { get; set; }

    [Column("DocumentosAnteriores")]
    public string? DocumentosAnteriores { get; set; }

    [Column("Observaciones")]
    public string? Observaciones { get; set; }

    [Column("DescripcionCambios")]
    public string? DescripcionCambios { get; set; }
}