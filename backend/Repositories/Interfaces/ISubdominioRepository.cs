using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface ISubdominioRepository : IRepository<Subdominio, int>
    {
        // Métodos específicos para Subdominio
        Task<IEnumerable<Subdominio>> ObtenerPorProcesoId(int procesoId);

        // Búsquedas con paginación
        Task<(IEnumerable<Subdominio> Items, int TotalCount)> FindByPracticasGobiernoContainingAsync(
            string practicasGobierno, int page, int pageSize);
        Task<(IEnumerable<Subdominio> Items, int TotalCount)> FindByIndicadoresAsociadosContainingAsync(
            string indicadoresAsociados, int page, int pageSize);

        // Incluir relaciones
        Task<Subdominio?> GetByIdWithActividadesAsync(int id);
        Task<Subdominio?> GetByIdWithProcesoAsync(int id);
        Task<IEnumerable<Subdominio>> GetAllWithProcesoAsync();
        Task<IEnumerable<Subdominio>> GetByProcesoIdWithActividadesAsync(int procesoId);
    }
}
