namespace backend.Services.Interfaces
{
    public interface IAuditoriaService
    {
        /// <summary>
        /// Registra un evento de auditoría en la base de datos.
        /// Si falla (ej. BD no disponible), solo registra en log — no lanza excepción.
        /// </summary>
        Task RegistrarEventoAsync(
            string tipoEvento,
            string descripcion,
            string? modulo = null,
            int? usuarioId = null,
            object? datosAnteriores = null,
            object? datosNuevos = null);
    }
}
