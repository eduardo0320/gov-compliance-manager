using backend.DTOs;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IDocumentoRepository : IRepository<Documento, int>
    {
        // Obtener todos los documentos de una actividad (no eliminados)
        Task<IEnumerable<Documento>> ObtenerPorActividadId(int actividadId);

        // Obtener documento con su versión actual cargada
        Task<Documento?> ObtenerConVersionActual(int id);

        // Obtener documento con todas sus versiones
        Task<Documento?> ObtenerConVersiones(int id);

        // Obtener documento con todas sus relaciones
        Task<Documento?> ObtenerConRelaciones(int id);

        // Obtener documentos próximos a vencer (dentro de N días)
        Task<IEnumerable<Documento>> ObtenerProximosAVencer(int dias);

        // Obtener documentos por estado (no eliminados)
        Task<IEnumerable<Documento>> ObtenerPorEstado(string estado);

        // Soft delete: marcar como eliminado
        Task<bool> EliminarLogico(int id, int usuarioId);

        // Verificar si ya existe un documento Principal en una actividad
        Task<bool> ExistePrincipalEnActividadAsync(int actividadId);

        // Buscar documentos con filtros dinámicos
        Task<IEnumerable<Documento>> BuscarConFiltrosAsync(BuscarDocumentosDto filtros);

        // Documentos vencidos con jerarquía completa (para alertas)
        Task<IEnumerable<Documento>> ObtenerVencidosConJerarquiaAsync();

        // Documentos próximos a vencer con jerarquía completa
        Task<IEnumerable<Documento>> ObtenerProximosAVencerConJerarquiaAsync(int dias);

        // Todos los documentos no eliminados (para estadísticas)
        Task<IEnumerable<Documento>> ObtenerTodosNoEliminadosAsync();

        // Documento público por id (no eliminado, Confidencialidad == Publica)
        Task<Documento?> ObtenerPublicoPorIdAsync(int id);

        // Documentos públicos no eliminados con versión actual cargada
        Task<IEnumerable<Documento>> ObtenerPublicosConVersionAsync();
    }
}