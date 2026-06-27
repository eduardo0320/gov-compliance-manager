using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("Subdominio")]
public class Subdominio
{
    [Key, Column("id_Subdominio")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdSubdominio { get; set; }

    [Required, MaxLength(255)]
    [Column("PracticasGobierno")]
    public string PracticasGobierno { get; set; } = null!;

    [Required, MaxLength(500)]
    [Column("indicadoresAsociados")]
    public string IndicadoresAsociados { get; set; } = null!;

    [Required]
    [Column("ProcesoId")]
    public int ProcesoId { get; set; }

    [ForeignKey(nameof(ProcesoId))]
    public Proceso Proceso { get; set; } = null!;

    // Subdominio → Actividades (1:N)
    public ICollection<Actividad> Actividades { get; set; } = new List<Actividad>();
}
