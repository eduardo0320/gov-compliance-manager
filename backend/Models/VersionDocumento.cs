using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("versiones_documento")]
public class VersionDocumento
{
    [Key, Column("id_VersionDocumento")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdVersionDocumento { get; set; }

    // FK circular a Documento — configurada vía Fluent API en NormasDb (ON DELETE CASCADE)
    [Required]
    [Column("DocumentoId")]
    public int DocumentoId { get; set; }

    public Documento Documento { get; set; } = null!;

    [Required]
    [Column("NumeroVersion")]
    public int NumeroVersion { get; set; }

    // Texto de versión: "1.0", "1.1", "2.0", etc.
    [Required, MaxLength(20)]
    [Column("VersionTexto")]
    public string VersionTexto { get; set; } = null!;

    // En BD es ENUM('Archivo','URL'); aquí como string
    [Required, MaxLength(10)]
    [Column("TipoAlmacenamiento")]
    public string TipoAlmacenamiento { get; set; } = null!;

    // Ruta relativa al repositorio de documentos
    [MaxLength(500)]
    [Column("RutaArchivo")]
    public string? RutaArchivo { get; set; }

    // URL externa (cuando TipoAlmacenamiento = 'URL')
    [Column("URL")]
    public string? Url { get; set; }

    [MaxLength(255)]
    [Column("NombreArchivoOriginal")]
    public string? NombreArchivoOriginal { get; set; }

    [Column("TamanoBytes")]
    public long? TamanoBytes { get; set; }

    [MaxLength(100)]
    [Column("MimeType")]
    public string? MimeType { get; set; }

    // Hash SHA-256 para verificación de integridad
    [MaxLength(64)]
    [Column("ChecksumSHA256")]
    public string? ChecksumSHA256 { get; set; }

    // Descripción de cambios en esta versión
    [Column("Comentario")]
    public string? Comentario { get; set; }

    // NOTA: No existe campo EsVersionActual.
    // La versión actual se determina exclusivamente desde Documento.VersionActualId.

    [Required]
    [Column("SubidoPorId")]
    public int SubidoPorId { get; set; }

    [ForeignKey(nameof(SubidoPorId))]
    public Usuario SubidoPor { get; set; } = null!;

    [Required]
    [Column("FechaSubida")]
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

    // Permite "eliminar" versiones sin borrarlas físicamente
    [Column("Activo")]
    public bool Activo { get; set; } = true;

    // Fecha de vencimiento de esta versión (cuando vence el documento en este punto)
    [Column("FechaVencimiento")]
    public DateTime? FechaVencimiento { get; set; }
}
