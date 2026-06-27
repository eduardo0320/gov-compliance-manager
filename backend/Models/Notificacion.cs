using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("Notificaciones")]
    public class Notificacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UsuarioDestinoId { get; set; }

        [ForeignKey(nameof(UsuarioDestinoId))]
        public Usuario UsuarioDestino { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Titulo { get; set; } = null!;

        [Required, MaxLength(1000)]
        public string Mensaje { get; set; } = null!;

        // "info", "success", "warning", "danger"
        [MaxLength(20)]
        public string Tipo { get; set; } = "info";

        // URL opcional para redirigir al hacer clic
        [MaxLength(500)]
        public string? UrlDestino { get; set; }

        public bool Leida { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaLectura { get; set; }
    }
}
