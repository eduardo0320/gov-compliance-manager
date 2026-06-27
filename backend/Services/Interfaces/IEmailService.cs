using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    public interface IEmailService
    {
        Task EnviarCorreoRegistro(string correo, string nombre, string contrasenaTemporal);
        Task EnviarCorreoRecuperacion(string correo, string nombre, string nuevaContrasena);

        Task EnviarCodigoRecuperacion(string correo, string nombre, string codigo);
        Task EnviarCodigoTwoFactor(string correo, string nombre, string codigo);

        /// <summary>Notifica al responsable de un documento que está próximo a vencer o ya venció.</summary>
        Task EnviarAlertaVencimientoDocumento(
            string correo,
            string nombre,
            string nombreDocumento,
            int diasRestantes,
            string actividadNombre);


        Task EnviarAlertaVencimientoActividad(
            string correo,
            string nombre,
            int diasRestantes,
            string actividadNombre);
    }

}
