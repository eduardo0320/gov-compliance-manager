using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IAuditoriaRepository : IRepository<Auditoria, int>
    {
        // Métodos específicos para Auditoria
        Task<IEnumerable<Auditoria>> FindByIdUsuarioAsync(int? idUsuario);
        Task<IEnumerable<Auditoria>> FindByTipoEventoAsync(string tipoEvento);
        Task<IEnumerable<Auditoria>> FindByFechaEventoRangeAsync(DateTime fechaDesde, DateTime fechaHasta);
        Task<IEnumerable<Auditoria>> FindByTablaAfectadaAsync(string tablaAfectada);
        Task<IEnumerable<Auditoria>> FindByRegistroAfectadoAsync(int registroAfectado);
        
        // Búsquedas con paginación
        Task<(IEnumerable<Auditoria> Items, int TotalCount)> FindByDescripcionContainingAsync(
            string descripcion, int page, int pageSize);
        Task<(IEnumerable<Auditoria> Items, int TotalCount)> FindByTipoEventoAsync(
            string tipoEvento, int page, int pageSize);
        Task<(IEnumerable<Auditoria> Items, int TotalCount)> FindByFechaEventoRangeAsync(
            DateTime fechaDesde, DateTime fechaHasta, int page, int pageSize);
            
        // Incluir relaciones
        Task<IEnumerable<Auditoria>> GetAllWithUsuarioAsync();
        Task<Auditoria?> GetByIdWithUsuarioAsync(int id);
        
        // Reportes de auditoría
        Task<IEnumerable<Auditoria>> GetUltimosEventosAsync(int cantidad);
        Task<IEnumerable<Auditoria>> GetEventosPorUsuarioAsync(int idUsuario, DateTime? fechaDesde = null);
        Task<Dictionary<string, int>> GetEstadisticasPorTipoEventoAsync(DateTime fechaDesde, DateTime fechaHasta);
    }
}
