namespace backend.Services.Interfaces;

public interface IHistorialActividadService
{
    Task RegistrarVersionAnteriorAsync(int actividadId, string descripcionCambios, int? usuarioModificacionId = null);
    Task<IEnumerable<object>> ObtenerHistorialPorActividadAsync(int actividadId);
}