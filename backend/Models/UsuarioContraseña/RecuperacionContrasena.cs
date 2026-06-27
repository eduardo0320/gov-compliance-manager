using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models.UsuarioContrasena
{
    public class RecuperacionContrasena
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Required, MaxLength(255)]
        public string CodigoHash { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiraEn { get; set; }

        public bool Usado { get; set; } = false;

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    }
}
