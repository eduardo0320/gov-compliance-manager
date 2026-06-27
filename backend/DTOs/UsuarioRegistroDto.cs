namespace backend.DTOs
{
    public class UsuarioRegistroDto
    {
        public string nombre { get; set; } = "";
        public string cedula { get; set; } = "";
        public string correo_electronico { get; set; } = "";
        public string? departamento { get; set; }
        public int idRol { get; set; }
    }
}
