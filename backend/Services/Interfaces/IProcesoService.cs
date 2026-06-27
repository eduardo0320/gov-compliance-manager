using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IProcesoService
    {
        // Operaciones CRUD básicas
        Task<IEnumerable<object>> ObtenerTodosLosProcesosAsync();
        Task<object?> ObtenerProcesoPorIdAsync(int id);
    Task<string> CrearProcesoAsync(string codigo, string nombre, string marcoNormativo, int dominioId, int creadoPorId, int? prioridadImplementacion = null);
    Task<string> ActualizarProcesoAsync(int id, string codigo, string nombre, string marcoNormativo, int dominioId, int modificadoPorId, int? prioridadImplementacion = null);
        Task<string> EliminarProcesoAsync(int id);
        
        // Operaciones de negocio específicas
        Task<bool> ExisteProcesoPorCodigoAsync(string codigo);
        Task<bool> ExisteProcesoPorNombreYDominioAsync(string nombre, int dominioId);
        Task<object?> ObtenerProcesoPorCodigoAsync(string codigo);
        Task<IEnumerable<object>> BuscarProcesosPorNombreAsync(string nombre);
        Task<IEnumerable<object>> ObtenerProcesosPorDominioAsync(int dominioId);
        
        // Operaciones de estado y avance
        Task<string> ActualizarEstadoImplementacionAsync(int id, string estado);
        Task<string> ActualizarPorcentajeAvanceAsync(int id, decimal porcentaje);
        Task<bool> TieneSubdominiosAsociadosAsync(int procesoId);
        Task<string> ActualizarPorcentajeActividadAsync(int actividadId, decimal porcentaje, int usuarioId);
        
        // Operaciones con relaciones
        Task<IEnumerable<object>> ObtenerProcesosConSubdominiosAsync();
        Task<object?> ObtenerProcesoConDetalleCompletoAsync(int id);
        Task<IEnumerable<object>> ObtenerProcesosConActividadesAsync();
        Task<int> ContarSubdominiosPorProcesoAsync(int procesoId);
        Task<int> ContarActividadesPorProcesoAsync(int procesoId);
        
        // Filtros y reportes
        Task<IEnumerable<object>> FiltrarProcesosPorEstadoAsync(string estado);
        Task<IEnumerable<object>> ObtenerProcesosPorRangoAvanceAsync(decimal minPorcentaje, decimal maxPorcentaje);
    }
}
