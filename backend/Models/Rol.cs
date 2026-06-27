using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace backend.Models
{
    public class Rol
    {
        [Key]
        public int idRol { get; set; }

        [Required]
        [MaxLength(100)]
        public string nombre { get; set; } = string.Empty;

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
