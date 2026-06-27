using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using backend.DTOs;
using backend.Models;
using backend.Services.Interfaces;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IRolService _rolService;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IAuditoriaService? _auditoriaService;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            IUsuarioService usuarioService,
            IRolService rolService,
            IConfiguration config,
            IEmailService emailService,
            ILogger<UsuariosController> logger,
            IAuditoriaService? auditoriaService = null)
        {
            _usuarioService = usuarioService;
            _rolService = rolService;
            _config = config;
            _emailService = emailService;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        /// <summary>
        /// Obtiene la lista completa de usuarios del sistema
        /// </summary>
        /// <returns>Lista de usuarios con información básica</returns>
        [HttpGet]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> ListarUsuarios()
        {
            try
            {
                var usuarios = await _usuarioService.ObtenerTodosLosUsuariosAsync();

                return Ok(new
                {
                    usuarios = usuarios,
                    total = usuarios.Count(),
                    cantidad = usuarios.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        /// <summary>
        /// Filtra usuarios según criterios especificados
        /// </summary>
        /// <param name="filtros">Criterios de filtrado</param>
        /// <returns>Lista filtrada de usuarios</returns>
        [HttpPost("filtrar")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> FiltrarUsuarios([FromBody] FiltroUsuariosDto filtros)
        {
            try
            {
                if (filtros == null)
                {
                    return BadRequest(new { mensaje = "Los filtros no pueden estar vacíos" });
                }

                // Usar el servicio que ya maneja el filtrado
                var usuariosFiltrados = await _usuarioService.FiltrarUsuariosAsync(filtros);

                return Ok(new
                {
                    usuarios = usuariosFiltrados,
                    total = usuariosFiltrados.Count(),
                    cantidad = usuariosFiltrados.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al filtrar usuarios", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un usuario específico por su cédula
        /// </summary>
        /// <param name="cedula">Cédula del usuario a buscar</param>
        /// <returns>Información del usuario encontrado</returns>
        [HttpGet("{cedula}")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> ObtenerUsuarioPorCedula(string cedula)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cedula))
                {
                    return BadRequest(new { mensaje = "La cédula es requerida" });
                }

                var usuario = await _usuarioService.ObtenerUsuarioPorCedulaAsync(cedula);
                if (usuario == null)
                {
                    return NotFound(new { mensaje = $"No se encontró un usuario con la cédula: {cedula}" });
                }

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el usuario", error = ex.Message });
            }
        }


        /// <summary>
        /// Cambia el estado (activo/inactivo) de un usuario
        /// </summary>
        /// <param name="cedula">Cédula del usuario</param>
        /// <returns>Resultado de la operación</returns>
        [HttpPut("{cedula}/estado")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> CambiarEstadoUsuario(string cedula)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cedula))
                {
                    return BadRequest(new { mensaje = "La cédula es requerida" });
                }

                var rolActual = User.Claims.FirstOrDefault(c => c.Type == "rol")?.Value ?? "";
                var cedulaActual = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value ?? "";

                // Usar el servicio que maneja toda la lógica de cambio de estado
                var resultado = await _usuarioService.CambiarEstadoUsuarioAsync(cedula);

                if (resultado.Contains("Error") || resultado.Contains("No se encontró") || resultado.Contains("No tiene permisos"))
                {
                    return BadRequest(new { mensaje = resultado });
                }

                // Determinar el nuevo estado basado en el mensaje de respuesta
                bool nuevoEstado = resultado.Contains("activado");

                return Ok(new
                {
                    mensaje = resultado,
                    nuevo_estado = nuevoEstado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al cambiar el estado del usuario", error = ex.Message });
            }
        }



        // REGISTRAR USUARIO 
        [HttpPost("registrar")]
        [Authorize]
        public async Task<IActionResult> RegistrarUsuario([FromBody] UsuarioRegistroDto dto)
        {
            var rolActual = User.Claims.FirstOrDefault(c => c.Type == "rol")?.Value;
            if (rolActual != "ADMIN" && rolActual != "SUPERADMIN")
                return Forbid();

            if (!Regex.IsMatch(dto.correo_electronico, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest(new { mensaje = "El correo electrónico no tiene un formato válido." });

            bool existeCedula = await _usuarioService.ExisteUsuarioPorCedulaAsync(dto.cedula);
            if (existeCedula)
                return BadRequest(new { mensaje = $"El usuario con cédula {dto.cedula} ya se encuentra registrado en el sistema." });

            bool existeCorreo = await _usuarioService.ExisteUsuarioPorCorreoAsync(dto.correo_electronico);
            if (existeCorreo)
                return BadRequest(new { mensaje = $"Ya existe un usuario registrado con el correo electrónico {dto.correo_electronico}." });

            // Crear usuario usando el servicio
            try
            {
                var contrasenaTemporal = await _usuarioService.CrearUsuarioAsync(dto);

                if (contrasenaTemporal.Contains("Error"))
                {
                    return BadRequest(new { mensaje = contrasenaTemporal });
                }

                // Enviar correo en segundo plano sin bloquear la respuesta
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.EnviarCorreoRegistro(dto.correo_electronico, dto.nombre, contrasenaTemporal);
                        _logger.LogInformation("Correo de registro enviado exitosamente a {Correo}", dto.correo_electronico);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Usuario creado correctamente pero falló el envío de correo a {Correo}", dto.correo_electronico);
                    }
                });

                return Ok(new { mensaje = "El usuario ha sido registrado exitosamente y se ha enviado un correo con las credenciales." });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) when (dbEx.InnerException is MySqlConnector.MySqlException mysqlEx)
            {

                // Manejo específico para errores de MySQL
                if (mysqlEx.ErrorCode == MySqlConnector.MySqlErrorCode.DuplicateKeyEntry)
                {
                    if (mysqlEx.Message.Contains("cedula"))
                    {
                        return BadRequest(new { mensaje = $"El usuario con cédula {dto.cedula} ya se encuentra registrado en el sistema." });
                    }
                    else if (mysqlEx.Message.Contains("correo_electronico"))
                    {
                        return BadRequest(new { mensaje = $"Ya existe un usuario registrado con el correo electrónico {dto.correo_electronico}." });
                    }
                    else
                    {
                        return BadRequest(new { mensaje = "Ya existe un usuario con la misma información en el sistema." });
                    }
                }

                return StatusCode(500, new { mensaje = "Error en la base de datos al registrar el usuario." });
            }
            catch (Exception ex)
            {
                // Regresa SIEMPRE JSON (no HTML), para que el frontend lo pueda leer.
                // En producción, no expongas ex.Message (logéalo y envía mensaje genérico).
                return StatusCode(500, new { mensaje = "No se pudo completar el registro. " + ex.Message });
            }
        }


        [HttpPut("cambiar-contrasena")]
        [Authorize]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaDto dto)
        {
            try
            {
                var cedula = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(cedula))
                {
                    return Unauthorized(new { mensaje = "No se pudo identificar al usuario" });
                }

                if (dto.NuevaContrasena != dto.ConfirmarContrasena)
                {
                    return BadRequest(new { mensaje = "La nueva contraseña y su confirmación no coinciden" });
                }

                if (!ValidarFormatoContrasena(dto.NuevaContrasena))
                {
                    return BadRequest(new
                    {
                        mensaje = "La nueva contraseña debe tener mínimo 8 caracteres, al menos una mayúscula, una minúscula, un número y un símbolo especial"
                    });
                }

                var resultado = await _usuarioService.CambiarContrasenaAsync(cedula, dto.ContrasenaActual, dto.NuevaContrasena);

                if (resultado == "CONTRASEÑA_INCORRECTA")
                {
                    return BadRequest(new { mensaje = "La contraseña actual es incorrecta" });
                }
                else if (resultado == "CONTRASEÑA_IGUAL")
                {
                    return BadRequest(new { mensaje = "La nueva contraseña debe ser diferente a la contraseña actual" });
                }
                else if (resultado == "USUARIO_NO_ENCONTRADO")
                {
                    return NotFound(new { mensaje = "Usuario no encontrado" });
                }
                else if (resultado == "SUCCESS")
                {
                    return Ok(new { mensaje = "Su contraseña ha sido cambiada exitosamente." });
                }
                else
                {
                    return StatusCode(500, new { mensaje = "Error al cambiar la contraseña" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña para usuario {Cedula}", User.Identity?.Name);
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Valida que la contraseña cumpla con los requisitos de seguridad
        /// </summary>
        /// <param name="contrasena">Contraseña a validar</param>
        /// <returns>True si cumple con los requisitos</returns>
        private bool ValidarFormatoContrasena(string contrasena)
        {
            if (string.IsNullOrEmpty(contrasena) || contrasena.Length < 8)
                return false;

            // Al menos una mayúscula
            if (!Regex.IsMatch(contrasena, @"[A-Z]"))
                return false;

            // Al menos una minúscula
            if (!Regex.IsMatch(contrasena, @"[a-z]"))
                return false;

            // Al menos un número
            if (!Regex.IsMatch(contrasena, @"[0-9]"))
                return false;

            // Al menos un símbolo especial
            if (!Regex.IsMatch(contrasena, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
                return false;

            return true;
        }

        /// <summary>
        /// Edita la información de un usuario existente
        /// </summary>
        /// <param name="cedula">Cédula del usuario a editar</param>
        /// <param name="dto">Datos actualizados del usuario (JSON desde frontend)</param>
        /// <returns>Resultado de la operación</returns>
        [HttpPut("editarUsuario/{cedula}")]
        [Authorize(Policy = "AdminOrSuperadmin")]
        public async Task<IActionResult> EditarInformacionAsync(string cedula, [FromBody] UsuarioEdicionDto dto)
        {
            try
            {
                // Validar que la cédula no esté vacía
                if (string.IsNullOrWhiteSpace(cedula))
                {
                    return BadRequest(new { mensaje = "La cédula del usuario es requerida" });
                }

                // Validar el DTO recibido
                if (dto == null)
                {
                    return BadRequest(new { mensaje = "Los datos del usuario son requeridos" });
                }

                // Validar formato de correo electrónico
                if (!Regex.IsMatch(dto.correo_electronico, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new { mensaje = "El correo electrónico no tiene un formato válido" });
                }

                // Obtener el usuario existente usando el repositorio a través del servicio
                var usuarioExistente = await _usuarioService.ObtenerUsuarioCompletoAsync(cedula);
                if (usuarioExistente == null)
                {
                    return NotFound(new { mensaje = $"No se encontró un usuario con la cédula: {cedula}" });
                }

                var datosAnteriores = new
                {
                    usuarioExistente.Id_Usuario,
                    usuarioExistente.cedula,
                    usuarioExistente.nombre,
                    usuarioExistente.correo_electronico,
                    usuarioExistente.departamento,
                    usuarioExistente.idRol,
                    usuarioExistente.estado
                };

                // Validar que la nueva cédula no esté en uso por otro usuario (si cambió)
                if (cedula != dto.cedula && await _usuarioService.ExisteUsuarioPorCedulaAsync(dto.cedula))
                {
                    return BadRequest(new { mensaje = $"Ya existe un usuario con la cédula: {dto.cedula}" });
                }

                // Crear objeto Usuario con los datos actualizados
                var usuarioActualizado = new Usuario
                {
                    cedula = dto.cedula.Trim(),
                    nombre = dto.nombre.Trim(),
                    correo_electronico = dto.correo_electronico.Trim(),
                    departamento = dto.departamento.Trim(),
                    idRol = dto.idRol
                };

                // Obtener el ID del usuario existente
                dynamic usuarioExistenteDynamic = usuarioExistente;
                string cedulaUsuario = usuarioExistenteDynamic.cedula;

                // Actualizar usando el servicio (usa ActualizarUsuarioAsync que está definido en la interfaz)
                var exitoso = await _usuarioService.ActualizarUsuarioAsync(cedulaUsuario, usuarioActualizado);

                if (exitoso)
                {
                    var usuarioActualizadoCompleto = await _usuarioService.ObtenerUsuarioCompletoAsync(usuarioActualizado.cedula);
                    var actorIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var actorId = int.TryParse(actorIdClaim, out var parsedActorId) ? parsedActorId : (int?)null;

                    if (_auditoriaService != null)
                    {
                        await _auditoriaService.RegistrarEventoAsync(
                            "Modificación",
                            $"Usuario editado: '{usuarioActualizado.nombre}' (cédula {usuarioActualizado.cedula})",
                            "Usuarios",
                            actorId,
                            datosAnteriores,
                            usuarioActualizadoCompleto == null ? null : new
                            {
                                usuarioActualizadoCompleto.Id_Usuario,
                                usuarioActualizadoCompleto.cedula,
                                usuarioActualizadoCompleto.nombre,
                                usuarioActualizadoCompleto.correo_electronico,
                                usuarioActualizadoCompleto.departamento,
                                usuarioActualizadoCompleto.idRol,
                                usuarioActualizadoCompleto.estado
                            });
                    }

                    return Ok(new { mensaje = "La información del usuario ha sido actualizada exitosamente" });
                }
                else
                {
                    return StatusCode(500, new { mensaje = "No se pudo actualizar la información del usuario" });
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar usuario con cédula: {Cedula}", cedula);
                return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
            }
        }

        // ===== HU-005: ENDPOINTS PARA EDITAR MI PROPIA INFORMACIÓN =====

        /// <summary>
        /// Obtiene el perfil del usuario autenticado
        /// </summary>
        /// <returns>Información del perfil del usuario actual</returns>
        [HttpGet("mi-perfil")]
        [Authorize]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            try
            {
                // Obtener cédula del usuario desde el token JWT
                var cedulaClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Name);
                if (cedulaClaim == null || string.IsNullOrWhiteSpace(cedulaClaim.Value))
                {
                    return Unauthorized(new { mensaje = "Token inválido o no se pudo identificar al usuario" });
                }

                string cedula = cedulaClaim.Value;

                var perfil = await _usuarioService.ObtenerUsuarioPorCedulaAsync(cedula);
                if (perfil == null)
                {
                    return NotFound(new { mensaje = "Usuario no encontrado" });
                }

                return Ok(new
                {
                    mensaje = "Perfil obtenido exitosamente",
                    perfil = perfil
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil del usuario");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualiza la información personal del usuario autenticado
        /// Solo puede editar: nombre, correo electrónico y departamento
        /// NO puede editar: cédula, rol, estado
        /// </summary>
        /// <param name="perfilDto">Datos actualizados del perfil</param>
        /// <returns>Resultado de la actualización</returns>
        [HttpPut("mi-perfil")]
        [Authorize]
        public async Task<IActionResult> ActualizarMiPerfil([FromBody] ActualizarMiPerfilDto perfilDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new
                    {
                        mensaje = "Datos inválidos",
                        errores = errores
                    });
                }

                // Obtener ID del usuario desde el token JWT
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { mensaje = "Token inválido o no se pudo identificar al usuario" });
                }

                var resultado = await _usuarioService.ActualizarMiPerfilAsync(userId, perfilDto);

                return resultado switch
                {
                    "SUCCESS" => Ok(new
                    {
                        mensaje = "Su información ha sido actualizada correctamente."
                    }),
                    "USUARIO_NO_ENCONTRADO" => NotFound(new
                    {
                        mensaje = "Usuario no encontrado"
                    }),
                    "CORREO_YA_EXISTE" => BadRequest(new
                    {
                        mensaje = "El correo electrónico ya está registrado por otro usuario"
                    }),
                    "ERROR_ACTUALIZAR" => BadRequest(new
                    {
                        mensaje = "No se pudo actualizar la información"
                    }),
                    _ => StatusCode(500, new
                    {
                        mensaje = "Error interno del servidor"
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil del usuario");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Cambia la contraseña del usuario autenticado
        /// Requiere contraseña actual para validación de seguridad
        /// </summary>
        /// <param name="contrasenaDto">Datos para cambio de contraseña</param>
        /// <returns>Resultado del cambio de contraseña</returns>
        [HttpPut("mi-contrasena")]
        [Authorize]
        public async Task<IActionResult> CambiarMiContrasena([FromBody] CambiarContrasenaDto contrasenaDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new
                    {
                        mensaje = "Datos inválidos",
                        errores = errores
                    });
                }

                // Obtener ID del usuario desde el token JWT
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { mensaje = "Token inválido o no se pudo identificar al usuario" });
                }

                var resultado = await _usuarioService.CambiarMiContrasenaAsync(userId, contrasenaDto);

                return resultado switch
                {
                    "SUCCESS" => Ok(new
                    {
                        mensaje = "Su contraseña ha sido cambiada exitosamente."
                    }),
                    "USUARIO_NO_ENCONTRADO" => NotFound(new
                    {
                        mensaje = "Usuario no encontrado"
                    }),
                    "CONTRASEÑA_ACTUAL_INCORRECTA" => BadRequest(new
                    {
                        mensaje = "La contraseña actual es incorrecta"
                    }),
                    "CONTRASEÑA_IGUAL" => BadRequest(new
                    {
                        mensaje = "La nueva contraseña debe ser diferente a la actual"
                    }),
                    _ => StatusCode(500, new
                    {
                        mensaje = "Error interno del servidor"
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña del usuario");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Cambia la contraseña obligatoria del usuario autenticado (HU-009)
        /// No requiere contraseña actual ya que es un cambio obligatorio
        /// Actualiza el flag DebeRestablecerContrasena a false
        /// </summary>
        /// <param name="nuevaContrasena">Nueva contraseña</param>
        /// <returns>Resultado del cambio obligatorio</returns>
        [HttpPut("restablecer-contrasena-obligatoria")]
        [Authorize]
        public async Task<IActionResult> RestablecerContrasenaObligatoria([FromBody] string nuevaContrasena)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nuevaContrasena))
                {
                    return BadRequest(new { mensaje = "La nueva contraseña es requerida" });
                }

                // Validaciones de contraseña
                if (nuevaContrasena.Length < 8)
                {
                    return BadRequest(new { mensaje = "La contraseña debe tener al menos 8 caracteres" });
                }

                if (!Regex.IsMatch(nuevaContrasena, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]"))
                {
                    return BadRequest(new
                    {
                        mensaje = "La contraseña debe contener al menos: 1 mayúscula, 1 minúscula, 1 número y 1 carácter especial"
                    });
                }

                // Obtener cédula del usuario desde el token JWT
                var cedulaClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Name);
                if (cedulaClaim == null || string.IsNullOrWhiteSpace(cedulaClaim.Value))
                {
                    return Unauthorized(new { mensaje = "Token inválido o no se pudo identificar al usuario" });
                }

                string cedula = cedulaClaim.Value;

                var resultado = await _usuarioService.RestablecerContrasenaObligatoriaPorCedulaAsync(cedula, nuevaContrasena);

                return resultado switch
                {
                    "SUCCESS" => Ok(new
                    {
                        mensaje = "Su contraseña ha sido cambiada exitosamente. Ya puede usar el sistema normalmente."
                    }),
                    "USUARIO_NO_ENCONTRADO" => NotFound(new
                    {
                        mensaje = "Usuario no encontrado"
                    }),
                    "NO_REQUIERE_CAMBIO" => BadRequest(new
                    {
                        mensaje = "No es necesario cambiar la contraseña"
                    }),
                    "MISMA_CONTRASENA" => BadRequest(new
                    {
                        mensaje = "La nueva contraseña debe ser diferente a su contraseña actual"
                    }),
                    _ => StatusCode(500, new
                    {
                        mensaje = "Error interno del servidor"
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña obligatoria del usuario {UserId}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }
    }
}