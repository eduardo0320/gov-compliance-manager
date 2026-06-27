namespace backend.DTOs
{
    public class FiltroUsuariosDto
    {
        public string? nombre { get; set; }
        public string? cedula { get; set; }
        public string? departamento { get; set; }
        public string? rol { get; set; }
        public string? estado { get; set; }
        public DateTime? fechaCreacion { get; set; }
    }
}
