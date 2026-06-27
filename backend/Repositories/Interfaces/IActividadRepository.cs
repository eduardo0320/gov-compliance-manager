using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IActividadRepository : IRepository<Actividad, int>
    {
        // Métodos específicos para Actividad
        Task<IEnumerable<Actividad>> ObtenerPorIdSubdominio(int subdominioId);
        Task<IEnumerable<Actividad>> ObtenerPorEstadoImplementacion(string estadoImplementacion);
        Task<IEnumerable<Actividad>> ObtenerPorImplementable(string implementable);
        Task<IEnumerable<Actividad>> ObtenerPorIdFuncionariosResponsables(int funcionarioId);
        Task<IEnumerable<Actividad>> ObtenerPorRangoDeFechaCompromiso(DateTime fechaDesde, DateTime fechaHasta);
        Task<IEnumerable<Actividad>> ObtenerPorRangoDePorcentajeAvance(decimal porcentajeMin, decimal porcentajeMax);

        // Búsquedas con paginación
        Task<(IEnumerable<Actividad> Items, int TotalCount)> FindByNombreContainingAsync(
            string nombre, int page, int pageSize);
        Task<(IEnumerable<Actividad> Items, int TotalCount)> EncontrarPorEstadoImplementacion(
            string estadoImplementacion, int page, int pageSize);
        Task<(IEnumerable<Actividad> Items, int TotalCount)> FindByObservacionesContainingAsync(
            string observaciones, int page, int pageSize);

        // Incluir relaciones
        Task<Actividad?> GetByIdWithFuncionarioResponsableAsync(int id);
        Task<Actividad?> GetByIdWithSubdominioAsync(int id);
        Task<IEnumerable<Actividad>> GetAllWithFuncionarioResponsableAsync();

        // Carga la jerarquía completa: Actividad → Subdominio → Proceso → Dominio
        // Necesario para construir rutas de almacenamiento de documentos
        Task<Actividad?> ObtenerPorIdConJerarquia(int id);

        // Reportes y estadísticas
        Task<IEnumerable<Actividad>> ObtenerActividadesPendientesAsync();

        Task<IEnumerable<Actividad>> ObtenerActividadesPendientesPorDiasVencimiento(int dias);
        Task<IEnumerable<Actividad>> ObtenerActividadesVencidasAsync();
        Task<decimal> ObtenerPromedioAvancePorSubdominioAsync(int subdominioId);
    }
}
