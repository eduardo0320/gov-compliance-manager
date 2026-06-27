using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Services.Interfaces;
using System.Security.Claims;

namespace backend.Controladores
{
    [ApiController]
    [Route("api/auth")]
    public class AutenticacionController : ControllerBase
    {
        private readonly IAutenticacionService _autenticacionService;
        private readonly IWebHostEnvironment _env;

        public AutenticacionController(IAutenticacionService autenticacionService, IWebHostEnvironment env)
        {
            _autenticacionService = autenticacionService;
            _env = env;
        }

        [HttpPost("login")]
        public async Task<IActionResult> IniciarSesion([FromBody] SolicitudInicioSesion solicitud)
        {
            try
            {
                var (exito, mensaje, usuario, token) = await _autenticacionService.IniciarSesionAsync(solicitud.cedula, solicitud.contrasena);
                if (!exito) return Unauthorized(new { success = false, message = mensaje });

                // Si es 2FA requerido, no seteamos cookie aún.
                if (mensaje == "2FA_REQUERIDO")
                {
                    return Ok(new { success = true, user = usuario, mensaje });
                }

                // Enviar cookie si se generó token
                if (!string.IsNullOrEmpty(token))
                {
                    Response.Cookies.Append("token", token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = !_env.IsDevelopment(),
                        SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(480)
                    });
                }

                return Ok(new { success = true, user = usuario, mensaje });
            }
            catch (Exception ex)
            {
                var errorMessage = _env.IsDevelopment()
                    ? ex.Message
                    : "Error al procesar la solicitud";
                return StatusCode(500, new { success = false, mensaje = errorMessage });
            }
        }

        [HttpPost("2fa/confirmar")]
        public async Task<IActionResult> ConfirmarTwoFactor([FromBody] SolicitudTwoFactor dto)
        {
            try
            {
                var (exito, mensajeVerificacion) = await _autenticacionService.VerificarCodigoTwoFactorAsync(dto.cedula, dto.codigo);
                if (!exito) return BadRequest(new { success = false, mensaje = mensajeVerificacion });

                var token = _autenticacionService.GenerarToken(dto.cedula);

                Response.Cookies.Append("token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !_env.IsDevelopment(),
                    SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(480)
                });

                return Ok(new { success = true, mensaje = "2FA verificado y sesión iniciada" });
            }
            catch (Exception ex)
            {
                var errorMessage = _env.IsDevelopment() ? ex.Message : "Error al procesar la solicitud";
                return StatusCode(500, new { success = false, mensaje = errorMessage });
            }
        }

        [HttpPost("2fa/activar")]
        [Authorize]
        public async Task<IActionResult> ActivarTwoFactor()
        {
            try
            {
                var cedula = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (string.IsNullOrWhiteSpace(cedula)) return Unauthorized(new { success = false, mensaje = "Token inválido" });

                var (exito, mensaje) = await _autenticacionService.EstablecerTwoFactorAsync(cedula, true);
                if (!exito) return BadRequest(new { success = false, mensaje = mensaje });

                return Ok(new { success = true, mensaje = "2FA activado. Recibirá un código en su correo cada vez que inicie sesión." });
            }
            catch (Exception ex)
            {
                var errorMessage = _env.IsDevelopment() ? ex.Message : "Error al procesar la solicitud";
                return StatusCode(500, new { success = false, mensaje = errorMessage });
            }
        }

        [HttpPost("2fa/desactivar")]
        [Authorize]
        public async Task<IActionResult> DesactivarTwoFactor()
        {
            try
            {
                var cedula = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (string.IsNullOrWhiteSpace(cedula)) return Unauthorized(new { success = false, mensaje = "Token inválido" });

                var (exito, mensaje) = await _autenticacionService.EstablecerTwoFactorAsync(cedula, false);
                if (!exito) return BadRequest(new { success = false, mensaje = mensaje });

                return Ok(new { success = true, mensaje = "2FA desactivado." });
            }
            catch (Exception ex)
            {
                var errorMessage = _env.IsDevelopment() ? ex.Message : "Error al procesar la solicitud";
                return StatusCode(500, new { success = false, mensaje = errorMessage });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Registrar([FromBody] SolicitudRegistro solicitud)
        {
            try
            {
                var (exito, mensaje) = await _autenticacionService.RegistrarUsuarioAsync(
                    solicitud.cedula, solicitud.nombre, solicitud.correo_electronico,
                    solicitud.departamento, solicitud.idRol);

                if (!exito)
                {
                    if (mensaje.Contains("ya existe")) return Conflict(mensaje);
                    else return BadRequest(mensaje);
                }
                return Ok(new { mensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("rol")]
        [Authorize]
        public IActionResult ObtenerRol()
        {
            try
            {
                var nombreUsuario = User.FindFirst(ClaimTypes.Name)?.Value;
                var rol = User.FindFirst("rol")?.Value;
                if (string.IsNullOrEmpty(rol)) return Unauthorized();
                return Ok(new { rol, usuario = nombreUsuario });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPost("vencimiento")]
        public IActionResult ObtenerVencimientoToken([FromBody] SolicitudVencimiento solicitud)
        {
            try
            {
                if (string.IsNullOrEmpty(solicitud.token)) return BadRequest("Token es requerido");
                var vencimiento = _autenticacionService.ObtenerVencimientoToken(solicitud.token);
                return Ok(new { vencimiento });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPost("bloquear/{cedula}")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> BloquearUsuario(string cedula)
        {
            try
            {
                await _autenticacionService.BloquearUsuarioAsync(cedula);
                return Ok(new { mensaje = "Usuario bloqueado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPost("desbloquear/{cedula}")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> DesbloquearUsuario(string cedula)
        {
            try
            {
                await _autenticacionService.DesbloquearUsuarioAsync(cedula);
                return Ok(new { mensaje = "Usuario desbloqueado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPost("cambiar-contrasena")]
        [Authorize]
        public async Task<IActionResult> CambiarContrasena([FromBody] SolicitudCambioContrasena solicitud)
        {
            try
            {
                var cedulaClaim = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(cedulaClaim)) return BadRequest("Usuario no v�lido");

                var exito = await _autenticacionService.CambiarContrasenaAsync(cedulaClaim, solicitud.contrasenaActual, solicitud.nuevaContrasena);
                if (!exito) return BadRequest("Contrase�a actual incorrecta");
                return Ok(new { mensaje = "Contrase�a cambiada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // ===== NUEVOS ENDPOINTS: Recuperaci�n por c�digo =====
        // NOTA: Estos endpoints est�n comentados porque los m�todos correspondientes
        [HttpPost("recuperacion/solicitar")]
        [AllowAnonymous]
        public async Task<IActionResult> SolicitarRecuperacion([FromBody] SolicitudRecuperacion dto)
        {
            var (exito, mensaje) = await _autenticacionService.SolicitarCodigoRecuperacionAsync(dto.cedula);
            if (!exito) return BadRequest(new { mensaje });
            return Ok(new { mensaje });
        }

        [HttpPost("recuperacion/confirmar")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmarRecuperacion([FromBody] ConfirmarRecuperacion dto)
        {
            var (exito, mensaje) = await _autenticacionService.ConfirmarCodigoRecuperacionAsync(
                dto.cedula, dto.codigo, dto.nuevaContrasena);
            if (!exito) return BadRequest(new { mensaje });
            return Ok(new { mensaje });
        }

        [HttpPost("logout")]
        public IActionResult CerrarSesion()
        {
            // ? Eliminar la cookie
            Response.Cookies.Delete("token");

            return Ok(new { success = true, message = "Sesi�n cerrada exitosamente" });
        }

    }


    // ===== DTOs =====

    public class SolicitudInicioSesion
    {
        public string cedula { get; set; } = string.Empty;
        public string contrasena { get; set; } = string.Empty;
    }

    public class SolicitudRegistro
    {
        public string cedula { get; set; } = string.Empty;
        public string contrasena { get; set; } = string.Empty;
        public string? nombre { get; set; }
        public string? correo_electronico { get; set; }
        public string? departamento { get; set; }
        public int idRol { get; set; } = 2;
    }

    public class SolicitudVencimiento
    {
        public string token { get; set; } = string.Empty;
    }

    public class SolicitudCambioContrasena
    {
        public string contrasenaActual { get; set; } = string.Empty;
        public string nuevaContrasena { get; set; } = string.Empty;
    }

    // DTOs para recuperación de contraseña
    public class SolicitudRecuperacion
    {
        public string cedula { get; set; } = string.Empty;
    }

    public class ConfirmarRecuperacion
    {
        public string cedula { get; set; } = string.Empty;
        public string codigo { get; set; } = string.Empty;
        public string nuevaContrasena { get; set; } = string.Empty;
    }

    public class SolicitudTwoFactor
    {
        public string cedula { get; set; } = string.Empty;
        public string codigo { get; set; } = string.Empty;
    }
}