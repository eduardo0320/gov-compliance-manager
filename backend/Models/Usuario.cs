using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        public int Id_Usuario { get; set; }

        [Required]
        [MaxLength(20)]
        public string cedula { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string correo_electronico { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? departamento { get; set; }

        [Required]
        public int idRol { get; set; }
        public Rol? Rol { get; set; }

        [Required]
        [MaxLength(255)]
        public string contrasena { get; set; } = string.Empty;

        public bool estado { get; set; } = true;

        public DateTime fechaCreacion { get; set; } = DateTime.Now;

        public DateTime fechaUltimaModificacion { get; set; } = DateTime.Now;

        public DateTime? ultimoAcceso { get; set; }

        public int intentosLoginFallidos { get; set; } = 0;

        public DateTime? fechaBloqueado { get; set; }

        // HU-009: Campo para cambio obligatorio de contraseña
        public bool DebeRestablecerContrasena { get; set; } = false;
        // HU-2FA: Habilita verificación por correo cada inicio de sesión
        public bool TwoFactorEnabled { get; set; } = false;
    }
}
