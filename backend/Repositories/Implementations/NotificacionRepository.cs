using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class NotificacionRepository : Repository<Notificacion, int>, INotificacionRepository
    {
        public NotificacionRepository(NormasDb context) : base(context)
        {
        }

        public async Task<IEnumerable<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId, bool soloNoLeidas = false, int limite = 50)
        {
            var query = _dbSet.Where(n => n.UsuarioDestinoId == usuarioId);

            if (soloNoLeidas)
                query = query.Where(n => !n.Leida);

            return await query
                .OrderByDescending(n => n.FechaCreacion)
                .Take(limite)
                .ToListAsync();
        }

        public async Task<int> ContarNoLeidasAsync(int usuarioId)
        {
            return await _dbSet.CountAsync(n => n.UsuarioDestinoId == usuarioId && !n.Leida);
        }

        public async Task<bool> ExisteNotificacionNoLeidaAsync(int usuarioId, string tipo)
        {
            return await _dbSet.AnyAsync(n =>
                n.UsuarioDestinoId == usuarioId &&
                !n.Leida &&
                n.Tipo == tipo);
        }
    }
}
