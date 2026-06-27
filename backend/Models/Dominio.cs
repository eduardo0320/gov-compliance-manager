using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("Dominio")]
public class Dominio
{
    [Key, Column("id_Dominio")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdDominio { get; set; }

    [Required, MaxLength(255)]
    [Column("Nombre")]
    public string Nombre { get; set; } = null!;

    // Dominio → Procesos (1:N)
    public ICollection<Proceso> Procesos { get; set; } = new List<Proceso>();
}
