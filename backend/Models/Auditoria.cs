using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend.Models;

namespace backend.Models
{
    [Table("Auditoria")]
    public class Auditoria
    {
        [Key]
        [Column("id_Auditoria")]
        public int IdAuditoria { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("descripcion")]
        public string Descripcion { get; set; } = null!;

        [Required]
        [Column("fecha_evento")]
        public DateTime FechaEvento { get; set; } = DateTime.UtcNow;

        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("tipo_evento")]
        public string TipoEvento { get; set; } = null!; // Creación, Modificación, Eliminación, Login, etc.

        [MaxLength(100)]
        [Column("modulo")]
        public string? Modulo { get; set; } // Procesos, Actividades, Usuarios, etc.

        [MaxLength(50)]
        [Column("direccion_ip")]
        public string? DireccionIp { get; set; }

        [MaxLength(500)]
        [Column("navegador")]
        public string? Navegador { get; set; }

        [Column("datos_anteriores")]
        public string? DatosAnteriores { get; set; } // JSON de los datos antes del cambio

        [Column("datos_nuevos")]
        public string? DatosNuevos { get; set; } // JSON de los datos después del cambio

        // Navegación
        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}
