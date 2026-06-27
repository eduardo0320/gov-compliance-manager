using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IActividadService
    {
        // Operaciones CRUD básicas
        Task<IEnumerable<object>> ObtenerTodasLasActividadesAsync();
        Task<object?> ObtenerActividadPorIdAsync(int id);
        Task<string> CrearActividadAsync(string nombre, string implementable, int funcionariosResponsablesId, int subdominioId);
        Task<string> ActualizarActividadAsync(int id, string nombre, string implementable, int? funcionariosResponsablesId, int subdominioId);
        Task<string> EliminarActividadAsync(int id);

        // Operaciones de negocio específicas
        Task<bool> ExisteActividadPorNombreYSubdominioAsync(string nombre, int subdominioId);
        Task<IEnumerable<object>> BuscarActividadesPorNombreAsync(string nombre);
        Task<IEnumerable<object>> ObtenerActividadesPorSubdominioAsync(int subdominioId);
        Task<IEnumerable<object>> ObtenerActividadesPorProcesoAsync(int procesoId);
        Task<IEnumerable<object>> ObtenerActividadesPorDominioAsync(int dominioId);

        // Operaciones de estado y avance
        Task<string> ActualizarEstadoImplementacionAsync(int id, string estado);
        Task<string> ActualizarPorcentajeAvanceAsync(int id, decimal porcentaje);
        Task<string> ActualizarFechaCompromisoAsync(int id, DateTime? fechaCompromiso);
        Task<string> ActualizarFechaControlAsync(int id, DateTime? fechaControl);
        Task<string> ActualizarDocumentosAsync(int id, string? documentos);
        Task<string> ActualizarObservacionesAsync(int id, string? observaciones);

        // Operaciones con relaciones
        Task<IEnumerable<object>> ObtenerActividadesConDetalleCompletoAsync();
        Task<object?> ObtenerActividadConDetalleCompletoAsync(int id);
        Task<IEnumerable<object>> ObtenerActividadesPorResponsableAsync(int funcionariosResponsablesId);

        // Filtros y reportes
        Task<IEnumerable<object>> FiltrarActividadesPorEstadoAsync(string estado);
        Task<IEnumerable<object>> FiltrarActividadesPorImplementableAsync(string implementable);
        Task<IEnumerable<object>> ObtenerActividadesPorRangoAvanceAsync(decimal minPorcentaje, decimal maxPorcentaje);
        Task<IEnumerable<object>> ObtenerActividadesPorFechaCompromisoAsync(DateTime fechaInicio, DateTime fechaFin);
        Task<IEnumerable<object>> ObtenerActividadesVencidasAsync();
        Task<IEnumerable<object>> ObtenerActividadesProximasAVencerAsync(int diasAntelacion);

        // Estadísticas
        Task<object> ObtenerEstadisticasActividadesAsync();
        Task<object> ObtenerEstadisticasPorSubdominioAsync(int subdominioId);

        // Mis actividades (sidebar HU-016)
        Task<object> ObtenerMisActividadesAsync(int usuarioId);

        Task<object> enviarCorreosVencimientoActividadesAsync();
    }
}
