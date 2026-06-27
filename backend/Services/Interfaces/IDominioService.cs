using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IDominioService
    {
        // Operaciones CRUD básicas
        Task<IEnumerable<object>> ObtenerTodosLosDominiosAsync();
        Task<object?> ObtenerDominioPorIdAsync(int id);
        Task<string> CrearDominioAsync(string nombre);
        Task<string> ActualizarDominioAsync(int id, string nombre);
        Task<string> EliminarDominioAsync(int id);
        
        // Operaciones de negocio específicas
        Task<bool> ExisteDominioPorNombreAsync(string nombre);
        Task<object?> ObtenerDominioPorNombreAsync(string nombre);
        Task<IEnumerable<object>> BuscarDominiosPorNombreAsync(string nombre);
        Task<bool> TieneProcesosAsociadosAsync(int dominioId);
        
        // Operaciones con relaciones
        Task<IEnumerable<object>> ObtenerDominiosConProcesosAsync();
        Task<IEnumerable<object>> ObtenerDominiosConSubdominiosAsync();
        Task<object?> ObtenerDominioConDetalleCompletoAsync(int id);
        Task<int> ContarProcesosPorDominioAsync(int dominioId);
    }
}
