using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("documentos")]
public class Documento
{
    [Key, Column("id_Documento")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdDocumento { get; set; }

    [Required, MaxLength(255)]
    [Column("Nombre")]
    public string Nombre { get; set; } = null!;

    [Column("Descripcion")]
    public string? Descripcion { get; set; }

    // En BD es ENUM('PDF','DOCX','PPTX','URL','OTRO'); aquí como string
    [Required, MaxLength(10)]
    [Column("TipoDocumento")]
    public string TipoDocumento { get; set; } = null!;

    [Required]
    [Column("ActividadId")]
    public int ActividadId { get; set; }

    [ForeignKey(nameof(ActividadId))]
    public Actividad Actividad { get; set; } = null!;

    // FK circular a VersionDocumento — nullable, se llena tras crear la primera versión.
    // Configurado exclusivamente vía Fluent API en NormasDb (IsRequired(false)).
    [Column("VersionActualId")]
    public int? VersionActualId { get; set; }

    public VersionDocumento? VersionActual { get; set; }

    // En BD es ENUM('Borrador','En_Revision','Aprobado','Vigente','Obsoleto','Archivado'); aquí como string
    [Required, MaxLength(20)]
    [Column("Estado")]
    public string Estado { get; set; } = "Borrador";

    [Column("FechaVencimiento")]
    public DateTime? FechaVencimiento { get; set; }

    [Column("FechaAlerta")]
    public DateTime? FechaAlerta { get; set; }

    [MaxLength(100)]
    [Column("Categoria")]
    public string? Categoria { get; set; }

    // En BD es ENUM('Publica','Interna','Confidencial','Restringida'); aquí como string
    [Required, MaxLength(20)]
    [Column("Confidencialidad")]
    public string Confidencialidad { get; set; } = "Interna";

    // Rol que ocupa el documento dentro de la actividad: 'Principal' (único por actividad) o 'Anexo'
    [Required, MaxLength(20)]
    [Column("RolEnActividad")]
    public string RolEnActividad { get; set; } = "Anexo";

    // --- Auditoría ---
    // Múltiples FK a Usuario — configuradas vía Fluent API en NormasDb para evitar ambigüedad
    [Required]
    [Column("CreadoPorId")]
    public int CreadoPorId { get; set; }

    public Usuario CreadoPor { get; set; } = null!;

    [Required]
    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("ModificadoPorId")]
    public int? ModificadoPorId { get; set; }

    public Usuario? ModificadoPor { get; set; }

    [Column("FechaModificacion")]
    public DateTime? FechaModificacion { get; set; }

    // Control de concurrencia optimista — TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6) en MySQL.
    // Configurado con ValueGeneratedOnAddOrUpdate en NormasDb.
    [Column("RowVersion")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime RowVersion { get; set; }

    // --- Soft Delete ---
    [Column("Eliminado")]
    public bool Eliminado { get; set; } = false;

    [Column("EliminadoPorId")]
    public int? EliminadoPorId { get; set; }

    public Usuario? EliminadoPor { get; set; }

    [Column("FechaEliminacion")]
    public DateTime? FechaEliminacion { get; set; }

    // --- Navegación ---
    public ICollection<VersionDocumento> Versiones { get; set; } = new List<VersionDocumento>();
    public ICollection<RelacionDocumento> RelacionesComoOrigen { get; set; } = new List<RelacionDocumento>();
    public ICollection<RelacionDocumento> RelacionesComoDestino { get; set; } = new List<RelacionDocumento>();
    public ICollection<MetadatoDocumento> Metadatos { get; set; } = new List<MetadatoDocumento>();
}
