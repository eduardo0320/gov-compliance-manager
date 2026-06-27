using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IProcesoRepository : IRepository<Proceso, int>
    {
        // Métodos específicos para Proceso
        Task<Proceso?> ObtenerPorCodigo(string codigo);
        Task<bool> ExistePorCodigo(string codigo);
        Task<IEnumerable<Proceso>> EncontrarPorIdDominio(int dominioId);
        Task<IEnumerable<Proceso>> EncontrarPorEstadoImplementacion(string estadoImplementacion);
        Task<IEnumerable<Subdominio>> ObtenerSubdominiosPorIdProceso(int procesoId);

        // Búsquedas con paginación
        Task<(IEnumerable<Proceso> Items, int TotalCount)> FindByNombreContainingAsync(
            string nombre, int page, int pageSize);
        Task<(IEnumerable<Proceso> Items, int TotalCount)> FindByMarcoNormativoContainingAsync(
            string marcoNormativo, int page, int pageSize);

        // Incluir relaciones
        Task<Proceso?> GetByIdWithSubdominiosAsync(int id);
        Task<Proceso?> GetByIdWithDominioAsync(int id);
        Task<IEnumerable<Proceso>> GetAllWithDominioAsync();
    }
}
