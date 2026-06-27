using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos
{
    public class CrearActividadRequest
    {
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El responsable es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un responsable válido")]
        public int FuncionariosResponsablesId { get; set; }
    }

    public class ActividadResponse
    {
        public int IdActividad { get; set; }
        public string Nombre { get; set; } = null!;
        public string Implementable { get; set; } = "Sí";
        public DateTime? FechaCompromiso { get; set; }
        public string EstadoImplementacion { get; set; } = "Pendiente";
        public decimal PorcentajeAvance { get; set; }
        public int? FuncionariosResponsablesId { get; set; }
        public DateTime? FechaControl { get; set; }
        public string? Documentos { get; set; }
        public string? Observaciones { get; set; }
        public int SubdominioId { get; set; }
        public bool TieneDocumentosVencidos { get; set; } = false;
    }
    public class ActualizarActividadRequest
    {
        [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
        public string? Nombre { get; set; }
        public string? Implementable { get; set; }                
        public DateTime? FechaCompromiso { get; set; }
        public string? EstadoImplementacion { get; set; }         
        public decimal? PorcentajeAvance { get; set; }            
        public int? FuncionariosResponsablesId { get; set; }
        public DateTime? FechaControl { get; set; }
        public string? Documentos { get; set; }
        public string? Observaciones { get; set; }
    }
}