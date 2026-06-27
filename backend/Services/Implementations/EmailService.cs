using System.Net.Mail;
using System.Net;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task EnviarCorreoRegistro(string correo, string nombre, string contrasenaTemporal)
        {
            try
            {
                var mensaje = CrearMensajeRegistro(correo, nombre, contrasenaTemporal);
                await EnviarCorreo(mensaje);
                _logger.LogInformation("Correo de registro enviado exitosamente a {Correo}", correo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de registro a {Correo}", correo);
                throw;
            }
        }

        public async Task EnviarCorreoRecuperacion(string correo, string nombre, string nuevaContrasena)
        {
            try
            {
                var mensaje = CrearMensajeRecuperacion(correo, nombre, nuevaContrasena);
                await EnviarCorreo(mensaje);
                _logger.LogInformation("Correo de recuperación enviado exitosamente a {Correo}", correo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de recuperación a {Correo}", correo);
                throw;
            }
        }

        public async Task EnviarCodigoRecuperacion(string correo, string nombre, string codigo)
        {
            try
            {
                var fromName = _config["Email:FromName"] ?? "Sistema Normas";
                var smtpUser = _config["Email:SmtpUser"];
                if (string.IsNullOrEmpty(smtpUser)) throw new InvalidOperationException("SmtpUser no configurado en appsettings");

                var mail = new MailMessage();
                mail.From = new MailAddress(smtpUser, fromName);
                mail.To.Add(correo);
                mail.Subject = "Código de recuperación de contraseña";
                mail.Body =
$@"Hola {nombre},

Recibimos una solicitud para restablecer su contraseña.
Su código de verificación es: {codigo}

¡¡¡IMPORTANTE!!!

Su contraseña debe contener mínimo 8 caracteres, al menos una mayúscula, 
una minúscula, números y un símbolo especial.

Este código vence en 45 minutos y solo puede usarse una vez.
Si usted no solicitó este cambio, ignore este mensaje.

Saludos,
Equipo del Sistema";

                await EnviarCorreo(mail);
                _logger.LogInformation("Código de recuperación enviado a {Correo}", correo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar código de recuperación a {Correo}", correo);
                throw;
            }
        }

        public async Task EnviarCodigoTwoFactor(string correo, string nombre, string codigo)
        {
            try
            {
                var fromName = _config["Email:FromName"] ?? "Sistema Normas";
                var smtpUser = _config["Email:SmtpUser"];
                if (string.IsNullOrEmpty(smtpUser)) throw new InvalidOperationException("SmtpUser no configurado en appsettings");

                var mail = new MailMessage();
                mail.From = new MailAddress(smtpUser, fromName);
                mail.To.Add(correo);
                mail.Subject = "Código de autenticación 2FA";
                mail.Body =
$@"Hola {nombre},

Este es su código de verificación 2FA (2 dígitos): {codigo}

El código expira en 5 minutos y es de un solo uso.

Saludos,
Equipo del Sistema";

                await EnviarCorreo(mail);
                _logger.LogInformation("Código 2FA enviado a {Correo}", correo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar código 2FA a {Correo}", correo);
                throw;
            }
        }

        public async Task EnviarAlertaVencimientoDocumento(
            string correo,
            string nombre,
            string nombreDocumento,
            int diasRestantes,
            string actividadNombre)
        {
            try
            {
                var fromName = _config["Email:FromName"] ?? "Sistema Normas";
                var smtpUser = _config["Email:SmtpUser"];

                // Si SMTP no está configurado, no enviar (modo desarrollo sin email)
                if (string.IsNullOrEmpty(smtpUser))
                {
                    _logger.LogWarning(
                        "Alerta de vencimiento no enviada a {Correo}: SmtpUser no configurado.", correo);
                    return;
                }

                var mail = new MailMessage();
                mail.From = new MailAddress(smtpUser, fromName);
                mail.To.Add(correo);

                var asunto = diasRestantes <= 0
                    ? $"⚠️ Documento vencido: {nombreDocumento}"
                    : $"⏰ Documento próximo a vencer ({diasRestantes} día(s)): {nombreDocumento}";
                mail.Subject = asunto;

                var estadoVenc = diasRestantes <= 0
                    ? $"venció hace {Math.Abs(diasRestantes)} día(s)"
                    : $"vence en {diasRestantes} día(s)";

                mail.Body =
$@"Hola {nombre},

Este es un aviso automático del sistema de gestión documental.

El siguiente documento {estadoVenc}:

  Documento : {nombreDocumento}
  Actividad : {actividadNombre}

Acciones sugeridas:
  - Si el documento sigue vigente, actualice su fecha de vencimiento.
  - Si ya no aplica, cámbielo al estado Obsoleto.
  - Si requiere actualización, suba una nueva versión.

Ingrese al sistema para gestionar el documento.

Saludos,
Sistema de Gestión Documental — {fromName}";

                await EnviarCorreo(mail);
                _logger.LogInformation(
                    "Alerta de vencimiento enviada a {Correo} para documento '{Doc}'",
                    correo, nombreDocumento);
            }
            catch (Exception ex)
            {
                // No rethrow — un error de email no debe detener el job de alertas
                _logger.LogError(ex,
                    "Error al enviar alerta de vencimiento a {Correo} para '{Doc}'",
                    correo, nombreDocumento);
            }
        }

        public async Task EnviarAlertaVencimientoActividad(
    string correo,
    string nombre,
    int diasRestantes,
    string actividadNombre)
        {
            try
            {
                var fromName = _config["Email:FromName"] ?? "Sistema Normas";
                var smtpUser = _config["Email:SmtpUser"];

                // Si SMTP no está configurado, no enviar (modo desarrollo sin email)
                if (string.IsNullOrEmpty(smtpUser))
                {
                    _logger.LogWarning(
                        "Alerta de vencimiento no enviada a {Correo}: SmtpUser no configurado.", correo);
                    return;
                }

                var mail = new MailMessage();
                mail.From = new MailAddress(smtpUser, fromName);
                mail.To.Add(correo);

                var asunto = diasRestantes < 0
                    ? $"Actividad vencida: {actividadNombre}"
                    : diasRestantes == 0 ? $"Actividad vence hoy: {actividadNombre}"
                    : $"Actividad próxima a vencer ({diasRestantes} día(s)): {actividadNombre}";
                mail.Subject = asunto;

                var estadoVenc = diasRestantes < 0
                    ? $"venció hace {Math.Abs(diasRestantes)} día(s)"
                    : diasRestantes == 0 ? "vence hoy"
                    : $"vence en {diasRestantes} día(s)";

                mail.Body =
$@"Hola {nombre},

Este es un aviso automático del sistema de gestión documental.

La siguiente actividad {estadoVenc}:

  Actividad : {actividadNombre}

Acciones sugeridas:
  - Si la actividad sigue vigente, actualice su fecha de compromiso.
  - Si ya no aplica, cámbiela al estado Cancelada.
  - Si requiere actualización, suba una nueva versión.

Ingrese al sistema para gestionar la actividad.

Saludos,
Sistema de Gestión Documental — {fromName}";

                await EnviarCorreo(mail);
                _logger.LogInformation(
                    "Alerta de vencimiento enviada a {Correo} para actividad '{Act}'",
                    correo, actividadNombre);
            }
            catch (Exception ex)
            {
                // No rethrow — un error de email no debe detener el job de alertas
                _logger.LogError(ex,
                    "Error al enviar alerta de vencimiento a {Correo} para '{Act}'",
                    correo, actividadNombre);
            }
        }

        // ----- Helpers -----

        private MailMessage CrearMensajeRegistro(string correo, string nombre, string contrasenaTemporal)
        {
            var fromName = _config["Email:FromName"] ?? "Sistema Normas";
            var smtpUser = _config["Email:SmtpUser"];
            if (string.IsNullOrEmpty(smtpUser)) throw new InvalidOperationException("SmtpUser no configurado en appsettings");

            var mail = new MailMessage();
            mail.From = new MailAddress(smtpUser, fromName);
            mail.To.Add(correo);
            mail.Subject = "Registro de usuario en el sistema";
            mail.Body = $@"Hola {nombre},

Su cuenta ha sido creada exitosamente.

Su contraseña temporal es: {contrasenaTemporal}

Puede iniciar sesión en el sistema usando esta contraseña.
Por motivos de seguridad, se recomienda cambiarla en la sección Perfil si así lo desea.

Saludos,
Equipo del Sistema";
            return mail;
        }

        private MailMessage CrearMensajeRecuperacion(string correo, string nombre, string nuevaContrasena)
        {
            var fromName = _config["Email:FromName"] ?? "Sistema Normas";
            var smtpUser = _config["Email:SmtpUser"];
            if (string.IsNullOrEmpty(smtpUser)) throw new InvalidOperationException("SmtpUser no configurado en appsettings");

            var mail = new MailMessage();
            mail.From = new MailAddress(smtpUser, fromName);
            mail.To.Add(correo);
            mail.Subject = "Recuperación de contraseña";
            mail.Body = $@"Hola {nombre},

Se ha generado una nueva contraseña temporal para su cuenta.
Su nueva contraseña temporal es: {nuevaContrasena}

Por motivos de seguridad, se recomienda cambiarla inmediatamente después de iniciar sesión.

Saludos,
Equipo del Sistema";
            return mail;
        }

        private async Task EnviarCorreo(MailMessage mensaje)
        {
            var smtpHost = _config["Email:SmtpHost"];
            var smtpPort = _config.GetValue<int>("Email:SmtpPort", 587);
            var smtpUser = _config["Email:SmtpUser"];
            var smtpPass = _config["Email:SmtpPass"];

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                throw new InvalidOperationException("Configuración de email no encontrada en appsettings");

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mensaje);
        }
    }
}
