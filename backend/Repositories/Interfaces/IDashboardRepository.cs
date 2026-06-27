using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        /// <summary>
        /// Retorna el árbol completo en UNA sola consulta SQL:
        /// Dominios → Procesos → Subdominios → Actividades
        /// </summary>
        Task<IEnumerable<Dominio>> ObtenerArbolCompletoAsync();
    }
}
