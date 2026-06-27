using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IAutenticacionService
    {
        // Autenticación
        Task<(bool exito, string mensaje, object? usuario, string? token)> IniciarSesionAsync(string cedula, string contrasena);
        Task<(bool exito, string mensaje)> RegistrarUsuarioAsync(string cedula, string? nombre, string? correoElectronico, string? departamento, int rolId);
        string GenerarToken(string cedula);
        string ObtenerVencimientoToken(string token);

        // Bloqueo/seguridad
        Task<bool> EstaBloqueadoAsync(string cedula);
        Task BloquearUsuarioAsync(string cedula);
        Task DesbloquearUsuarioAsync(string cedula);
        Task ResetearIntentosFallidosAsync(string cedula);

        Task ActualizarUltimoAccesoAsync(string cedula);

        // Contraseña
        Task<bool> ValidarContrasenaAsync(string cedula, string contrasena);
        Task<bool> CambiarContrasenaAsync(string cedula, string contrasenaActual, string nuevaContrasena);

        // Recuperación por código
        Task<(bool exito, string mensaje)> SolicitarCodigoRecuperacionAsync(string cedula);
        Task<(bool exito, string mensaje)> ConfirmarCodigoRecuperacionAsync(string cedula, string codigo, string nuevaContrasena);

        // 2FA
        Task<(bool exito, string mensaje)> SolicitarCodigoTwoFactorAsync(string cedula);
        Task<(bool exito, string mensaje)> VerificarCodigoTwoFactorAsync(string cedula, string codigo);

        Task<(bool exito, string mensaje)> EstablecerTwoFactorAsync(string cedula, bool habilitar);
    }
}
