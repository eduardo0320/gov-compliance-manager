using backend.Models;

namespace backend.Services.Interfaces
{
    public interface ISubdominioService
    {
        // Operaciones CRUD básicas
        Task<IEnumerable<object>> ObtenerTodosLosSubdominiosAsync();
        Task<object?> ObtenerSubdominioPorIdAsync(int id);
        Task<string> CrearSubdominioAsync(string practicasGobierno, string indicadoresAsociados, int procesoId);
        Task<string> ActualizarSubdominioAsync(int id, string practicasGobierno, string indicadoresAsociados, int procesoId);
        Task<string> EliminarSubdominioAsync(int id);
        
        // Operaciones de negocio específicas
        Task<bool> ExisteSubdominioPorPracticasYProcesoAsync(string practicasGobierno, int procesoId);
        Task<IEnumerable<object>> BuscarSubdominiosPorPracticasAsync(string practicas);
        Task<IEnumerable<object>> ObtenerSubdominiosPorProcesoAsync(int procesoId);
        Task<bool> TieneActividadesAsociadasAsync(int subdominioId);
        
        // Operaciones con relaciones
        Task<IEnumerable<object>> ObtenerSubdominiosConActividadesAsync();
        Task<object?> ObtenerSubdominioConDetalleCompletoAsync(int id);
        Task<IEnumerable<object>> ObtenerSubdominiosConProcesoYDominioAsync();
        Task<int> ContarActividadesPorSubdominioAsync(int subdominioId);
        
        // Filtros y reportes
        Task<IEnumerable<object>> FiltrarSubdominiosPorIndicadoresAsync(string indicadores);
        Task<IEnumerable<object>> ObtenerSubdominiosPorDominioAsync(int dominioId);
    }
}
