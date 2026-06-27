using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface INotificacionRepository : IRepository<Notificacion, int>
    {
        Task<IEnumerable<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId, bool soloNoLeidas = false, int limite = 50);
        Task<int> ContarNoLeidasAsync(int usuarioId);
        Task<bool> ExisteNotificacionNoLeidaAsync(int usuarioId, string tipo);
    }
}
