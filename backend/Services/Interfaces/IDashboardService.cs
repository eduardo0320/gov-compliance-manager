namespace backend.Services.Interfaces
{
    public interface IDashboardService
    {
        /// <summary>
        /// Árbol completo listo para serializar al frontend.
        /// </summary>
        Task<IEnumerable<object>> ObtenerArbolCompletoAsync();

        /// <summary>
        /// Conteos globales para las stat-cards del dashboard.
        /// </summary>
        Task<object> ObtenerStatsAsync();
    }
}
