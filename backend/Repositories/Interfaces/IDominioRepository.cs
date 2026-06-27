using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IDominioRepository : IRepository<Dominio, int>
    {
        // Métodos específicos para Dominio
        Task<Dominio?> FindByNombreAsync(string nombre);
        Task<bool> ExistsByNombreAsync(string nombre);
        Task<IEnumerable<Proceso>> GetProcesosByDominioIdAsync(int dominioId);
        
        // Búsquedas con paginación
        Task<(IEnumerable<Dominio> Items, int TotalCount)> FindByNombreContainingAsync(
            string nombre, int page, int pageSize);
            
        // Incluir procesos relacionados
        Task<Dominio?> GetByIdWithProcesosAsync(int id);
        Task<IEnumerable<Dominio>> GetAllWithProcesosAsync();
    }
}
