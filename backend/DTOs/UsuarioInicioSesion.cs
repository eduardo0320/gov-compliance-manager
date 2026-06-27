namespace backend.DTOs
{
    public class UsuarioInicioSesion
    {
        public string cedula { get; set; } = string.Empty;
        public string contrasena { get; set; } = string.Empty;
        public bool debeRestablecerContrasena { get; set; }

        public string correo_electronico { get; set; } = string.Empty;
        public string estado { get; set; } = string.Empty;
        public int intentosLoginFallidos { get; set; }
        public DateTime? fechaBloqueado { get; set; }

    }
}










