namespace backend.DTOs
{
    /// <summary>
    /// DTO para editar información de un usuario existente
    /// </summary>
    public class UsuarioEdicionDto
    {
        public string cedula { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string correo_electronico { get; set; } = string.Empty;
        public string departamento { get; set; } = string.Empty;
        public int idRol { get; set; }
    }
}
