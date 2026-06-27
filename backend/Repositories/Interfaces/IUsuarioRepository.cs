using backend.Models.UsuarioContrasena;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IUsuarioRepository : IRepository<Usuario, int>
    {
        // Métodos específicos para Usuario (similar al proyecto Java)
        Task<Usuario?> EncontrarPorCedula(string cedula);
        Task<Usuario?> EncontrarPorCorreoElectronico(string correoElectronico);
        Task<IEnumerable<Usuario>> EncontrarPorDepartamento(string departamento);
        Task<IEnumerable<Usuario>> EncontrarPorIdRol(int rolId);
        Task<bool> ExistePorCedula(string cedula);
        Task<bool> ExistePorCorreoElectronico(string correoElectronico);

        // Búsquedas con paginación 
        Task<(IEnumerable<Usuario> Items, int TotalCount)> BuscarPorNombreConteniendo(
            string nombre, int page, int pageSize);
        Task<(IEnumerable<Usuario> Items, int TotalCount)> EncontrarPorDepartamentoConteniendo(
            string departamento, int page, int pageSize);

        // Métodos específicos para edición de perfil propio (HU-005)
        Task<bool> ExistePorCorreoElectronicoExceptoUsuarioAsync(string correoElectronico, int userId);
        Task<bool> ActualizarMiPerfilAsync(int userId, string nombre, string correoElectronico, string? departamento);

        Task<bool> ActualizarintentosLoginFallidosAsync(string cedula, int intentos);

        Task<bool> ActualizarFechaBloqueadoAsync(string cedula, DateTime fecha);

    }
}
