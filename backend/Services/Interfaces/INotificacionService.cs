using backend.Models;

namespace backend.Services.Interfaces
{
    public interface INotificacionService
    {
        Task CrearNotificacionAsync(int usuarioDestinoId, string titulo, string mensaje, string tipo = "info", string? urlDestino = null);
        Task GenerarNotificacionesVencimientoActividadesUsuarioAsync(int usuarioId);
        Task<IEnumerable<object>> ObtenerNotificacionesUsuarioAsync(int usuarioId, bool soloNoLeidas = false);
        Task<int> ContarNoLeidasAsync(int usuarioId);
        Task MarcarComoLeidaAsync(int notificacionId, int usuarioId);
        Task MarcarTodasComoLeidasAsync(int usuarioId);
        Task<bool> EliminarNotificacionAsync(int notificacionId, int usuarioId);
        Task EliminarTodasNotificacionesAsync(int usuarioId);
        Task<IEnumerable<object>> ObtenerUsuariosSinActividadesAsync();
        Task NotificarAdminsSobreEditoresSinActividadesAsync();
    }
}