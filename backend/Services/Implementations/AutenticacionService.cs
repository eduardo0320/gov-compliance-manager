using backend.Models.UsuarioContrasena;
using backend.Models;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace backend.Services.Implementations
{
    public class AutenticacionService : IAutenticacionService
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IRolService _rolService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AutenticacionService> _logger;
        private readonly IAuditoriaService? _auditoriaService;

        // NUEVO
        private readonly IEmailService _emailService;
        private readonly NormasDb _db;

        public AutenticacionService(
            IUsuarioService usuarioService,
            IRolService rolService,
            IConfiguration configuration,
            ILogger<AutenticacionService> logger,
            IEmailService emailService,
            NormasDb db,
            IAuditoriaService? auditoriaService = null)
        {
            _usuarioService = usuarioService;
            _rolService = rolService;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
            _db = db;
            _auditoriaService = auditoriaService;
        }

        public async Task<(bool exito, string mensaje, object? usuario, string? token)> IniciarSesionAsync(string cedula, string contrasena)
        {
            try
            {
                if (string.IsNullOrEmpty(cedula) || string.IsNullOrEmpty(contrasena))
                    return (false, "Cédula y contraseña son requeridas", null, null);

                var usuario = await _usuarioService.obtenerDatosInicioSesion(cedula);
                var usuarioCompleto = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);

                if (usuario == null || usuarioCompleto == null)
                {
                    Console.WriteLine($"Usuario con cédula {cedula} no encontrado durante inicio de sesión.");
                    return (false, "Credenciales inválidas", null, null);
                }

                if (usuario.fechaBloqueado.HasValue)
                    return (false, "Usuario bloqueado por múltiples intentos fallidos. Contacte al administrador.", null, null);

                if (usuario.estado == "Inactivo")
                    return (false, "Usuario inactivo, contacte al administrador", null, null);


                bool contrasenaValida = await ValidarContrasenaAsync(usuario.contrasena, contrasena);

                if (!contrasenaValida)
                {
                    await IncrementarIntentosFallidosAsync(usuario.cedula);
                    return (false, "Credenciales inválidas", null, null);
                }

                await ResetearIntentosFallidosAsync(usuario.cedula);
                await ActualizarUltimoAccesoAsync(usuario.cedula);

                if (usuario.debeRestablecerContrasena)
                {
                    Console.WriteLine($"Usuario {usuario.cedula} debe cambiar contraseña obligatoriamente. Redirigiendo a cambio de contraseña.");

                    // Generar token temporal para permitir el cambio de contraseña
                    var tokenTemporal = GenerarToken(cedula);

                    var usuarioLimitado = new
                    {
                        cedula = usuario.cedula,
                        debeRestablecerContrasena = true
                    };

                    return (true, "CAMBIO_CONTRASENA_REQUERIDO", usuarioLimitado, tokenTemporal);
                }

                if (usuarioCompleto.TwoFactorEnabled)
                {
                    var (exito2fa, mensaje2fa) = await SolicitarCodigoTwoFactorAsync(usuarioCompleto.cedula);
                    if (!exito2fa)
                    {
                        return (false, mensaje2fa, null, null);
                    }

                    return (true, "2FA_REQUERIDO", null, null);
                }

                var tokenNormal = GenerarToken(cedula);
                var usuarioPerfil = await _usuarioService.ObtenerMiPerfilAsync(usuario.cedula);

                if (usuarioPerfil == null)
                {
                    return (false, "Error al obtener información del usuario", null, null);
                }

                var usuarioNormal = new
                {
                    idUsuario = usuarioPerfil.Id_Usuario,
                    cedula = usuarioPerfil.cedula,
                    nombre = usuarioPerfil.nombre,
                    correo_electronico = usuarioPerfil.correo_electronico,
                    departamento = usuarioPerfil.departamento,
                    nombreRol = usuarioPerfil.nombreRol,
                    rol = usuarioPerfil.nombreRol,
                    estado = usuarioPerfil.estado,
                    fechaCreacion = usuarioPerfil.fechaCreacion,
                    fechaUltimaModificacion = usuarioPerfil.fechaUltimaModificacion,
                    debeRestablecerContrasena = false
                };

                return (true, "Login exitoso", usuarioNormal, tokenNormal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en inicio de sesión para cédula: {Cedula}", cedula);
                return (false, "Error interno del servidor", null, null);
            }
        }

        public async Task<(bool exito, string mensaje)> RegistrarUsuarioAsync(string cedula, string? nombre, string? correoElectronico, string? departamento, int rolId)
        {
            try
            {
                if (string.IsNullOrEmpty(cedula))
                    return (false, "Cédula es requerida");

                if (await _usuarioService.ExisteUsuarioPorCedulaAsync(cedula))
                    return (false, "El usuario ya existe");

                var rol = await _rolService.ObtenerRolPorId(rolId);
                if (rol == null)
                    return (false, "El rol especificado no existe");

                var nuevoUsuario = new DTOs.UsuarioRegistroDto
                {
                    cedula = cedula,
                    nombre = nombre ?? "Usuario",
                    correo_electronico = correoElectronico ?? "",
                    departamento = departamento ?? "",
                    idRol = rolId,
                };

                await _usuarioService.CrearUsuarioAsync(nuevoUsuario);
                return (true, "Usuario creado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario con cédula: {Cedula}", cedula);
                return (false, "Error interno del servidor");
            }
        }

        public string GenerarToken(string cedula)
        {
            var usuario = _usuarioService.ObtenerUsuarioCompletoAsync(cedula).Result;

            if (usuario == null)
            {
                throw new InvalidOperationException($"Usuario con cédula {cedula} no encontrado");
            }

            try
            {
                var jwt = _configuration.GetSection("JWT");
                var clave = Encoding.UTF8.GetBytes(jwt["Key"] ?? "SuperSecretKey12345_MiCITT_Sistema_Normas_2024");

                string nombreRol = string.Empty;
                if (usuario.Rol != null)
                {
                    nombreRol = usuario.Rol.nombre;
                }
                else
                {
                    var rol = _rolService.ObtenerRolPorId(usuario.idRol).Result;
                    nombreRol = rol?.nombre ?? "";
                }

                var reclamos = new[]
                {
                    new Claim(ClaimTypes.Name, usuario.cedula),
                    new Claim("rol", nombreRol),
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id_Usuario.ToString())
                };

                var credenciales = new SigningCredentials(new SymmetricSecurityKey(clave), SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: jwt["Issuer"] ?? "backend",
                    audience: jwt["Audience"] ?? "frontend",
                    claims: reclamos,
                    expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiryInMinutes"] ?? "30")),
                    signingCredentials: credenciales
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando token para usuario: {UsuarioId}", usuario.Id_Usuario);
                throw;
            }
        }

        public string ObtenerVencimientoToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                var expClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "exp");
                if (expClaim == null) return "Token sin fecha de vencimiento";
                if (long.TryParse(expClaim.Value, out long exp))
                {
                    return (exp * 1000).ToString();
                }
                return "Formato de vencimiento inválido";
            }
            catch (Exception ex)
            {
                return $"Error al procesar token: {ex.Message}";
            }
        }

        public async Task<bool> EstaBloqueadoAsync(string cedula)
        {
            try
            {
                if (!await _usuarioService.EstaActivo(cedula))
                    return true;

                if (!await _usuarioService.EstaBloqueado(cedula))
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verificando bloqueo para cédula {cedula}: {ex.Message}");
                return false;
            }
        }

        public async Task BloquearUsuarioAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario != null)
                {
                    var datosAnteriores = new
                    {
                        usuario.estado,
                        usuario.intentosLoginFallidos,
                        usuario.fechaBloqueado
                    };

                    usuario.fechaBloqueado = DateTime.UtcNow;
                    usuario.estado = false;
                    await _usuarioService.ActualizarUsuarioAsync(cedula, usuario);

                    await RegistrarAuditoriaSafeAsync(
                        "Desactivación",
                        $"Usuario bloqueado manualmente: '{usuario.nombre}' (cédula {usuario.cedula})",
                        "Autenticacion",
                        usuarioId: null,
                        datosAnteriores: datosAnteriores,
                        datosNuevos: new
                        {
                            usuario.estado,
                            usuario.intentosLoginFallidos,
                            usuario.fechaBloqueado,
                            motivo = "Bloqueo manual"
                        });

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bloqueando usuario: {Cedula}", cedula);
            }
        }

        public async Task DesbloquearUsuarioAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario != null)
                {
                    var datosAnteriores = new
                    {
                        usuario.estado,
                        usuario.intentosLoginFallidos,
                        usuario.fechaBloqueado
                    };

                    usuario.fechaBloqueado = null;
                    usuario.intentosLoginFallidos = 0;
                    usuario.estado = true;
                    await _usuarioService.ActualizarUsuarioAsync(cedula, usuario);

                    await RegistrarAuditoriaSafeAsync(
                        "Activación",
                        $"Usuario desbloqueado: '{usuario.nombre}' (cédula {usuario.cedula})",
                        "Autenticacion",
                        usuarioId: null,
                        datosAnteriores: datosAnteriores,
                        datosNuevos: new
                        {
                            usuario.estado,
                            usuario.intentosLoginFallidos,
                            usuario.fechaBloqueado
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desbloqueando usuario: {Cedula}", cedula);
            }
        }

        public async Task ResetearIntentosFallidosAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario != null && (usuario.intentosLoginFallidos > 0 || usuario.fechaBloqueado != null))
                {
                    usuario.intentosLoginFallidos = 0;
                    usuario.fechaBloqueado = null;
                    await _usuarioService.ActualizarUsuarioAsync(cedula, usuario);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reseteando intentos fallidos: {Cedula}", cedula);
            }
        }



        public async Task ActualizarUltimoAccesoAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario != null)
                {
                    usuario.ultimoAcceso = DateTime.Now;
                    await _usuarioService.ActualizarUsuarioAsync(cedula, usuario);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando último acceso: {Cedula}", cedula);
            }
        }

        /// <summary>
        /// Verifica si la contraseña ingresada coincide con la hasheada en BD
        /// </summary>
        /// <param name="cedula">Cédula del usuario</param>
        /// <param name="contrasenaTextoPlano">Contraseña en texto plano ingresada por el usuario</param>
        /// <returns>True si la contraseña es correcta, False en caso contrario</returns>
        public Task<bool> ValidarContrasenaAsync(string contrasenaHasheada, string contrasenaTextoPlano)
        {
            try
            {

                if (contrasenaHasheada == null)
                {
                    _logger.LogWarning("Contraseña hasheada no proporcionada para verificación");
                    return Task.FromResult(false);
                }

                bool esValida = BCrypt.Net.BCrypt.Verify(contrasenaTextoPlano, contrasenaHasheada);
                _logger.LogInformation("Verificación de contraseña: {Resultado}",
                    esValida ? "Exitosa" : "Fallida");

                return Task.FromResult(esValida);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando contraseña");
                return Task.FromResult(false);
            }
        }

        public async Task<bool> CambiarContrasenaAsync(string cedula, string contrasenaActual, string nuevaContrasena)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario == null) return false;
                if (!BCrypt.Net.BCrypt.Verify(contrasenaActual, usuario.contrasena)) return false;

                usuario.contrasena = BCrypt.Net.BCrypt.HashPassword(nuevaContrasena);
                usuario.fechaUltimaModificacion = DateTime.Now;
                await _usuarioService.ActualizarUsuarioAsync(usuario.cedula, usuario);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando contraseña: {Cedula}", cedula);
                return false;
            }
        }

        // ===== NUEVO: Recuperación por código =====

        private static bool CumplePolitica(string pass)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                pass, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$");
        }

        public async Task<(bool exito, string mensaje)> SolicitarCodigoRecuperacionAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario == null || !usuario.estado)
                    return (false, "No se encontró un usuario activo con esa cédula.");

                if (string.IsNullOrWhiteSpace(usuario.correo_electronico))
                    return (false, "El usuario no tiene correo electrónico registrado. Contacte al administrador.");

                var rnd = new Random();
                var codigo = rnd.Next(100000, 999999).ToString();

                var hash = BCrypt.Net.BCrypt.HashPassword(codigo);

                var registro = new backend.Models.UsuarioContrasena.RecuperacionContrasena
                {
                    UsuarioId = usuario.Id_Usuario,
                    CodigoHash = hash,
                    ExpiraEn = DateTime.UtcNow.AddMinutes(45)
                };

                _db.RecuperacionesContrasena.Add(registro);
                await _db.SaveChangesAsync();

                await _emailService.EnviarCodigoRecuperacion(usuario.correo_electronico, usuario.nombre ?? "Usuario", codigo);

                return (true, $"Se ha enviado un código de verificación al correo registrado ({EnmascararCorreo(usuario.correo_electronico)}).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar código de recuperación para cédula {Cedula}", cedula);
                return (false, "Error interno del servidor. Intente nuevamente.");
            }
        }

        public async Task<(bool exito, string mensaje)> SolicitarCodigoTwoFactorAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario == null || !usuario.estado)
                    return (false, "No se encontró un usuario activo con esa cédula.");

                if (string.IsNullOrWhiteSpace(usuario.correo_electronico))
                    return (false, "El usuario no tiene correo electrónico registrado. Contacte al administrador.");

                var rnd = new Random();
                var codigo = rnd.Next(10, 100).ToString("D2");

                var hash = BCrypt.Net.BCrypt.HashPassword(codigo);

                var registro = new backend.Models.UsuarioContrasena.TwoFactorCode
                {
                    UsuarioId = usuario.Id_Usuario,
                    CodigoHash = hash,
                    ExpiraEn = DateTime.UtcNow.AddMinutes(5),
                    Usado = false
                };

                _db.TwoFactorCodes.Add(registro);
                await _db.SaveChangesAsync();

                await _emailService.EnviarCodigoTwoFactor(usuario.correo_electronico, usuario.nombre ?? "Usuario", codigo);

                return (true, $"Se ha enviado un código de 2 dígitos al correo registrado ({EnmascararCorreo(usuario.correo_electronico)}). ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar código 2FA para cédula {Cedula}", cedula);
                return (false, "Error interno del servidor. Intente nuevamente.");
            }
        }

        public async Task<(bool exito, string mensaje)> VerificarCodigoTwoFactorAsync(string cedula, string codigo)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario == null || !usuario.estado)
                    return (false, "Usuario inválido o inactivo.");

                var registro = await _db.TwoFactorCodes
                    .Where(r => r.UsuarioId == usuario.Id_Usuario && !r.Usado && r.ExpiraEn >= DateTime.UtcNow)
                    .OrderByDescending(r => r.CreadoEn)
                    .FirstOrDefaultAsync();

                if (registro == null)
                    return (false, "Código inválido o expirado.");

                if (!BCrypt.Net.BCrypt.Verify(codigo, registro.CodigoHash))
                    return (false, "Código inválido o expirado.");

                registro.Usado = true;
                await _db.SaveChangesAsync();

                return (true, "Código 2FA verificado correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando código 2FA para cédula {Cedula}", cedula);
                return (false, "Error interno del servidor. Intente nuevamente.");
            }
        }

        public async Task<(bool exito, string mensaje)> EstablecerTwoFactorAsync(string cedula, bool habilitar)
        {
            var result = await _usuarioService.EstablecerTwoFactorAsync(cedula, habilitar);
            if (result) return (true, habilitar ? "2FA activado" : "2FA desactivado");
            return (false, "No se pudo actualizar el estado de 2FA");
        }

        private string EnmascararCorreo(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo) || !correo.Contains("@"))
                return "***";

            var partes = correo.Split('@');
            var nombre = partes[0];
            var dominio = partes[1];

            if (nombre.Length <= 2)
                return $"{nombre[0]}***@{dominio}";

            return $"{nombre.Substring(0, 2)}***@{dominio}";
        }

        public async Task<(bool exito, string mensaje)> ConfirmarCodigoRecuperacionAsync(string cedula, string codigo, string nuevaContrasena)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario == null || !usuario.estado)
                    return (false, "No se encontró un usuario activo con esa cédula.");

                if (!CumplePolitica(nuevaContrasena))
                    return (false, "La nueva contraseña no cumple con los requisitos de seguridad (mínimo 8 caracteres, mayúscula, minúscula, número y símbolo).");

                var rec = await _db.RecuperacionesContrasena
                    .Where(r => r.UsuarioId == usuario.Id_Usuario && !r.Usado)
                    .OrderByDescending(r => r.CreadoEn)
                    .FirstOrDefaultAsync();

                if (rec == null || rec.ExpiraEn <= DateTime.UtcNow)
                    return (false, "Código inválido o expirado.");

                if (!BCrypt.Net.BCrypt.Verify(codigo, rec.CodigoHash))
                    return (false, "Código inválido o expirado.");

                rec.Usado = true;
                usuario.contrasena = BCrypt.Net.BCrypt.HashPassword(nuevaContrasena);
                usuario.fechaUltimaModificacion = DateTime.Now;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Contraseña recuperada exitosamente para usuario con cédula {Cedula}", cedula);
                return (true, "Su contraseña ha sido cambiada exitosamente. Puede iniciar sesión.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar código de recuperación para cédula {Cedula}", cedula);
                return (false, "Error interno del servidor. Intente nuevamente.");
            }
        }

        // ===== Helpers internos =====

        private async Task IncrementarIntentosFallidosAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuario no encontrado al incrementar intentos fallidos: {Cedula}", cedula);
                    return;
                }

                var intentosAnteriores = usuario.intentosLoginFallidos;
                usuario.intentosLoginFallidos++;

                if (usuario.intentosLoginFallidos >= 5)
                {
                    usuario.fechaBloqueado = DateTime.UtcNow;
                    usuario.estado = false;
                }

                await _usuarioService.ActualizarUsuarioAsync(cedula, usuario);

                if (usuario.intentosLoginFallidos == 3)
                {
                    await RegistrarAuditoriaSafeAsync(
                        "IntentosFallidosLogin",
                        $"Múltiples intentos fallidos de inicio de sesión para cédula {usuario.cedula}: {usuario.intentosLoginFallidos}",
                        "Autenticacion",
                        usuarioId: usuario.Id_Usuario,
                        datosAnteriores: new { intentosLoginFallidos = intentosAnteriores },
                        datosNuevos: new { intentosLoginFallidos = usuario.intentosLoginFallidos });
                }

                if (usuario.intentosLoginFallidos == 5)
                {
                    await RegistrarAuditoriaSafeAsync(
                        "Desactivación",
                        $"Usuario bloqueado automáticamente por intentos fallidos: '{usuario.nombre}' (cédula {usuario.cedula})",
                        "Autenticacion",
                        usuarioId: usuario.Id_Usuario,
                        datosAnteriores: new
                        {
                            estado = true,
                            intentosLoginFallidos = intentosAnteriores,
                            fechaBloqueado = (DateTime?)null
                        },
                        datosNuevos: new
                        {
                            usuario.estado,
                            usuario.intentosLoginFallidos,
                            usuario.fechaBloqueado,
                            motivo = "Bloqueo automático por múltiples intentos fallidos"
                        });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementando intentos fallidos para el usuario con cédula: {Cedula}", cedula);
            }
        }

        private async Task<bool> VerificarCambioDeContrasenaObligatorioAsync(string cedula)
        {
            var usuario = await _usuarioService.obtenerDatosInicioSesion(cedula);

            if (usuario != null && usuario.debeRestablecerContrasena)
            {
                Console.WriteLine($"Usuario {usuario.cedula} debe cambiar contraseña obligatoriamente.");
                return true;
            }
            return false;
        }

        private async Task RegistrarAuditoriaSafeAsync(
            string tipoEvento,
            string descripcion,
            string modulo,
            int? usuarioId,
            object? datosAnteriores = null,
            object? datosNuevos = null)
        {
            if (_auditoriaService == null)
                return;

            try
            {
                await _auditoriaService.RegistrarEventoAsync(
                    tipoEvento,
                    descripcion,
                    modulo,
                    usuarioId,
                    datosAnteriores,
                    datosNuevos);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo registrar auditoría en autenticación. TipoEvento={TipoEvento}",
                    tipoEvento);
            }
        }





    }
}