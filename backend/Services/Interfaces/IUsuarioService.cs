using backend.DTOs;
using backend.Models.UsuarioContrasena;
using backend.Models;


namespace backend.Services.Interfaces
{
    public interface IUsuarioService
    {
        // Operaciones CRUD
        Task<IEnumerable<object>> ObtenerTodosLosUsuariosAsync();
        Task<object?> ObtenerUsuarioPorCedulaAsync(string cedula); // Para API - retorna objeto anónimo
        Task<Usuario?> ObtenerUsuarioCompletoAsync(string cedula); // Para uso interno - retorna Usuario completo
        Task<object?> ObtenerUsuarioPorIdAsync(int id);
        Task<string> CrearUsuarioAsync(UsuarioRegistroDto usuarioDtoa);

        Task<string> CrearUsuarioInicialAsync();
        Task<bool> ActualizarUsuarioAsync(string cedula, Usuario usuario);
        Task<bool> EliminarUsuarioAsync(int id);

        // Operaciones de negocio específicas
        Task<string> CambiarEstadoUsuarioAsync(string cedula);
        Task<IEnumerable<object>> FiltrarUsuariosAsync(FiltroUsuariosDto filtros);
        Task<bool> ExisteUsuarioPorCedulaAsync(string cedula);
        Task<bool> ExisteUsuarioPorCorreoAsync(string correoElectronico);

        // Operaciones de autenticación y seguridad
        Task<int> ContarSuperAdminsActivosAsync();
        Task<string> CambiarContrasenaAsync(string cedula, string contrasenaActual, string nuevaContrasena);
        Task<bool> VerificarUsuarioPorDefecto();

        // Métodos específicos para HU-005: Editar mi propia información
        Task<MiPerfilDto?> ObtenerMiPerfilAsync(string cedula);
        Task<string> ActualizarMiPerfilAsync(int userId, ActualizarMiPerfilDto perfilDto);
        Task<string> CambiarMiContrasenaAsync(int userId, CambiarContrasenaDto contrasenaDto);

        // HU-009: Restablecimiento obligatorio de contraseña
        Task<string> RestablecerContrasenaObligatoriaAsync(int userId, string nuevaContrasena);
        Task<string> RestablecerContrasenaObligatoriaPorCedulaAsync(string cedula, string nuevaContrasena);

        Task<bool> EstaActivo(string cedula);

        Task<bool> EstaBloqueado(string cedula);
        Task<UsuarioInicioSesion?> obtenerDatosInicioSesion(string cedula);

        // HU-2FA: Ajustes de 2FA
        Task<bool> EstablecerTwoFactorAsync(string cedula, bool habilitar);
        Task<bool> EstaHabilitadoTwoFactorAsync(string cedula);

    }
}
