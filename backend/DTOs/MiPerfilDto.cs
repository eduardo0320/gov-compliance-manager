namespace backend.DTOs
{
    /// <summary>
    /// DTO para mostrar información del perfil del usuario autenticado
    /// No incluye información sensible como contraseñas
    /// </summary>
    public class MiPerfilDto
    {
        public int Id_Usuario { get; set; }
        public string cedula { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string correo_electronico { get; set; } = string.Empty;
        public string? departamento { get; set; }
        public string nombreRol { get; set; } = string.Empty;
        public bool estado { get; set; }
        public DateTime fechaCreacion { get; set; }
        public DateTime fechaUltimaModificacion { get; set; }
        public DateTime? ultimoAcceso { get; set; }
    }
}
