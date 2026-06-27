using backend.Models.UsuarioContrasena;
using backend.Models;
using backend.DTOs;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;


namespace backend.Services.Implementations
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IRolService _rolService;
        private readonly IAuditoriaService? _auditoriaService;
        private readonly ILogger<UsuarioService> _logger;

        private readonly IConfiguration _config;

        public UsuarioService(
            IUsuarioRepository usuarioRepository,
            IRolService rolService,
            ILogger<UsuarioService> logger,
            IConfiguration config,
            IAuditoriaService? auditoriaService = null)
        {
            _usuarioRepository = usuarioRepository;
            _rolService = rolService;
            _logger = logger;
            _config = config;
            _auditoriaService = auditoriaService;
        }

        // Operaciones CRUD
        public async Task<IEnumerable<object>> ObtenerTodosLosUsuariosAsync()
        {
            try
            {
                var usuarios = await _usuarioRepository.ObtenerTodos();
                return usuarios.Select(u => new
                {
                    id = u.Id_Usuario,
                    nombre_completo = u.nombre,
                    cedula = u.cedula,
                    correo_electronico = u.correo_electronico,
                    departamento = u.departamento,
                    rol_asignado = u.Rol?.nombre ?? "Sin rol",
                    estado = u.estado ? "Activo" : "Inactivo",
                    fechaCreacion = u.fechaCreacion,
                    ultimoAcceso = u.ultimoAcceso
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los usuarios");
                throw;
            }
        }

        public async Task<object?> ObtenerUsuarioPorCedulaAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioRepository.EncontrarPorCedula(cedula);
                if (usuario == null) return null;

                return new
                {
                    Id_Usuario = usuario.Id_Usuario,
                    nombre = usuario.nombre,
                    cedula = usuario.cedula,
                    correo_electronico = usuario.correo_electronico,
                    departamento = usuario.departamento,
                    nombreRol = usuario.Rol?.nombre ?? "Sin rol asignado",
                    estado = usuario.estado,
                    fechaCreacion = usuario.fechaCreacion,
                    fechaUltimaModificacion = usuario.fechaUltimaModificacion,
                    ultimoAcceso = usuario.ultimoAcceso,
                    twoFactorEnabled = usuario.TwoFactorEnabled
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por cédula: {Cedula}", cedula);
                throw;
            }
        }

        /// <summary>
        /// Obtiene el objeto Usuario completo por cédula (para uso interno)
        /// </summary>
        public async Task<Usuario?> ObtenerUsuarioCompletoAsync(string cedula)
        {
            try
            {
                return await _usuarioRepository.EncontrarPorCedula(cedula);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario completo por cédula: {Cedula}", cedula);
                throw;
            }
        }

        public async Task<object?> ObtenerUsuarioPorIdAsync(int id)
        {
            try
            {
                var usuario = await _usuarioRepository.ObtenerPorId(id);
                if (usuario == null) return null;

                return new
                {
                    id = usuario.Id_Usuario,
                    nombre_completo = usuario.nombre,
                    cedula = usuario.cedula,
                    correo_electronico = usuario.correo_electronico,
                    departamento = usuario.departamento,
                    rol = new
                    {
                        id = usuario.idRol,
                        nombre = usuario.Rol?.nombre ?? "Sin rol asignado"
                    },
                    estado = usuario.estado ? "Activo" : "Inactivo",
                    fechaCreacion = usuario.fechaCreacion,
                    fechaUltimaModificacion = usuario.fechaUltimaModificacion,
                    ultimoAcceso = usuario.ultimoAcceso,
                    intentosLoginFallidos = usuario.intentosLoginFallidos,
                    fechaBloqueado = usuario.fechaBloqueado
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por ID: {Id}", id);
                throw;
            }
        }

        public async Task<string> CrearUsuarioAsync(UsuarioRegistroDto usuarioDto)
        {
            try
            {
                // Validaciones de negocio
                if (await ExisteUsuarioPorCedulaAsync(usuarioDto.cedula))
                    throw new InvalidOperationException($"Ya existe un usuario con la cédula: {usuarioDto.cedula}");

                if (await ExisteUsuarioPorCorreoAsync(usuarioDto.correo_electronico))
                    throw new InvalidOperationException($"Ya existe un usuario con el correo: {usuarioDto.correo_electronico}");

                // Verificar que el rol existe
                var rol = await _rolService.ObtenerRolPorId(usuarioDto.idRol);
                if (rol == null)
                    throw new InvalidOperationException($"El rol con ID {usuarioDto.idRol} no existe");

                // Generar contraseña temporal
                var contrasenaTemp = GenerarContrasenaTemporal();

                var usuario = new Usuario
                {
                    cedula = usuarioDto.cedula,
                    nombre = usuarioDto.nombre,
                    correo_electronico = usuarioDto.correo_electronico,
                    departamento = usuarioDto.departamento,
                    idRol = usuarioDto.idRol,
                    contrasena = HashearContrasena(contrasenaTemp),
                    estado = true,
                    fechaCreacion = DateTime.Now,
                    fechaUltimaModificacion = DateTime.Now,
                    DebeRestablecerContrasena = true // HU-009: Usuarios creados por admin deben cambiar contraseña
                };

                await _usuarioRepository.Agregar(usuario);
                await _usuarioRepository.GuardarCambios();

                await RegistrarAuditoriaSafeAsync(
                    "Creación",
                    $"Usuario creado: '{usuario.nombre}' (cédula {usuario.cedula})",
                    "Usuarios",
                    datosNuevos: new
                    {
                        usuario.Id_Usuario,
                        usuario.cedula,
                        usuario.nombre,
                        usuario.correo_electronico,
                        usuario.idRol,
                        usuario.estado
                    });

                return contrasenaTemp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario: {Cedula}", usuarioDto.cedula);
                throw;
            }
        }

        public async Task<string> CrearUsuarioInicialAsync()
        {
            await _rolService.validarRolesExistentes();
            var rolAdmin = await _rolService.ObtenerRolPorNombre("SUPERADMIN");

            var usuario = new Usuario
            {
                cedula = _config["InitialAdminUser:Cedula"] ?? "000000000",
                nombre = _config["InitialAdminUser:Nombre"] ?? "Administrador Inicial",
                correo_electronico = _config["InitialAdminUser:Email"] ?? "admin@example.com",
                departamento = _config["InitialAdminUser:Departamento"] ?? "Dirección General",
                idRol = rolAdmin?.idRol ?? 1,      // usar idRol del rolAdmin o 1 por defecto
                contrasena = HashearContrasena(_config["InitialAdminUser:Password"] ?? "admin1234"), // usar contraseña de configuración o "admin1234" por defecto
                estado = true,
                fechaCreacion = DateTime.Now,
                fechaUltimaModificacion = DateTime.Now,
                DebeRestablecerContrasena = false // HU-009: Usuarios creados por admin deben cambiar contraseña
            };

            try
            {
                await _usuarioRepository.Agregar(usuario);
                await _usuarioRepository.GuardarCambios();

                return "superadmin1234";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario inicial: {Cedula}", usuario.cedula);
                throw;
            }
        }



        public async Task<bool> ActualizarUsuarioAsync(string cedula, Usuario usuario)
        {
            try
            {
                var usuarioExistente = await _usuarioRepository.EncontrarPorCedula(cedula);
                if (usuarioExistente == null) return false;

                // Validar que la cédula no esté en uso por otro usuario
                if (usuarioExistente.cedula != usuario.cedula &&
                    await ExisteUsuarioPorCedulaAsync(usuario.cedula))
                    throw new InvalidOperationException($"Ya existe un usuario con la cédula: {usuario.cedula}");

                // Validar que el correo no esté en uso por otro usuario
                if (usuarioExistente.correo_electronico != usuario.correo_electronico &&
                    await ExisteUsuarioPorCorreoAsync(usuario.correo_electronico))
                    throw new InvalidOperationException($"Ya existe un usuario con el correo: {usuario.correo_electronico}");

                // Verificar que el rol existe
                var rol = await _rolService.ObtenerRolPorId(usuario.idRol);
                if (rol == null)
                    throw new InvalidOperationException($"El rol con ID {usuario.idRol} no existe");

                // Actualizar propiedades
                usuarioExistente.cedula = usuario.cedula; // Actualizar cédula
                usuarioExistente.nombre = usuario.nombre;
                usuarioExistente.correo_electronico = usuario.correo_electronico;
                usuarioExistente.departamento = usuario.departamento;
                usuarioExistente.idRol = usuario.idRol;
                usuarioExistente.fechaUltimaModificacion = DateTime.Now;
                usuarioExistente.estado = usuario.estado;
                usuarioExistente.fechaBloqueado = usuario.fechaBloqueado;
                usuarioExistente.intentosLoginFallidos = usuario.intentosLoginFallidos;

                await _usuarioRepository.Actualizar(usuarioExistente);
                await _usuarioRepository.GuardarCambios();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario: {Cedula}", cedula);
                throw;
            }
        }

        public async Task<bool> EliminarUsuarioAsync(int id)
        {
            try
            {
                var usuario = await _usuarioRepository.ObtenerPorId(id);
                if (usuario == null) return false;

                var datosAnteriores = new
                {
                    usuario.Id_Usuario,
                    usuario.cedula,
                    usuario.nombre,
                    usuario.correo_electronico,
                    usuario.idRol,
                    usuario.estado
                };

                await _usuarioRepository.Eliminar(id);
                await _usuarioRepository.GuardarCambios();

                await RegistrarAuditoriaSafeAsync(
                    "Eliminación",
                    $"Usuario eliminado: '{usuario.nombre}' (cédula {usuario.cedula})",
                    "Usuarios",
                    datosAnteriores: datosAnteriores);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario: {Id}", id);
                throw;
            }
        }

        public async Task<string> CambiarEstadoUsuarioAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioRepository.EncontrarPorCedula(cedula);
                if (usuario == null) throw new InvalidOperationException("Usuario no encontrado");

                // Validar que no se desactive al último SUPERADMIN
                if (await EsUltimoSuperAdminActivo(usuario))
                {
                    throw new InvalidOperationException("No se puede desactivar al último súper administrador activo del sistema");
                }

                var estadoAnterior = usuario.estado;
                usuario.estado = !usuario.estado;
                usuario.fechaUltimaModificacion = DateTime.Now;

                // Si se activa, resetear intentos de login fallidos
                if (usuario.estado)
                {
                    usuario.intentosLoginFallidos = 0;
                    usuario.fechaBloqueado = null;
                }

                await _usuarioRepository.Actualizar(usuario);
                await _usuarioRepository.GuardarCambios();

                if (!usuario.estado)
                {
                    await RegistrarAuditoriaSafeAsync(
                        "Desactivación",
                        $"Usuario desactivado manualmente: '{usuario.nombre}' (cédula {usuario.cedula})",
                        "Usuarios",
                        datosAnteriores: new { estado = estadoAnterior },
                        datosNuevos: new
                        {
                            estado = usuario.estado,
                            motivo = "Desactivación manual"
                        });
                }
                else
                {
                    await RegistrarAuditoriaSafeAsync(
                        "Activación",
                        $"Usuario activado: '{usuario.nombre}' (cédula {usuario.cedula})",
                        "Usuarios",
                        datosAnteriores: new { estado = estadoAnterior },
                        datosNuevos: new { estado = usuario.estado });
                }

                return usuario.estado ? "Usuario activado exitosamente" : "Usuario desactivado exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del usuario: {Cedula}", cedula);
                throw;
            }
        }

        // Operaciones de búsqueda y filtrado
        public async Task<IEnumerable<object>> FiltrarUsuariosAsync(FiltroUsuariosDto filtros)
        {
            try
            {
                var todosLosUsuarios = await _usuarioRepository.ObtenerTodos();
                var usuariosFiltrados = AplicarFiltros(todosLosUsuarios, filtros);

                return usuariosFiltrados.Select(u => new
                {
                    id = u.Id_Usuario,
                    nombre_completo = u.nombre,
                    cedula = u.cedula,
                    correo_electronico = u.correo_electronico,
                    departamento = u.departamento,
                    rol_asignado = u.Rol?.nombre ?? "Sin rol",
                    estado = u.estado ? "Activo" : "Inactivo",
                    fechaCreacion = u.fechaCreacion,
                    ultimoAcceso = u.ultimoAcceso
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al filtrar usuarios");
                throw;
            }
        }

        // Operaciones de validación
        public async Task<bool> ExisteUsuarioPorCedulaAsync(string cedula)
        {
            try
            {
                return await _usuarioRepository.EncontrarPorCedula(cedula) != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de usuario por cédula: {Cedula}", cedula);
                throw;
            }
        }

        public async Task<bool> ExisteUsuarioPorCorreoAsync(string correo)
        {
            try
            {
                return await _usuarioRepository.ExistePorCorreoElectronico(correo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de usuario por correo: {Correo}", correo);
                throw;
            }
        }

        // Operaciones de auditoría
        public async Task<int> ContarSuperAdminsActivosAsync()
        {
            try
            {
                var rolSuperAdmin = await _rolService.ObtenerRolPorNombre("SUPERADMIN");
                if (rolSuperAdmin == null) return 0;

                var superAdmins = await _usuarioRepository.EncontrarPorIdRol(rolSuperAdmin.idRol);
                return superAdmins.Count(u => u.estado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al contar super administradores activos");
                throw;
            }
        }


        // Métodos privados
        private async Task<bool> EsUltimoSuperAdminActivo(Usuario usuario)
        {
            if (usuario.Rol?.nombre != "SUPERADMIN" || !usuario.estado)
                return false;

            var cantidadSuperAdminsActivos = await ContarSuperAdminsActivosAsync();
            return cantidadSuperAdminsActivos <= 1;
        }

        private IEnumerable<Usuario> AplicarFiltros(IEnumerable<Usuario> usuarios, FiltroUsuariosDto filtros)
        {
            var query = usuarios.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtros.nombre))
            {
                query = query.Where(u => u.nombre.Contains(filtros.nombre, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtros.cedula))
            {
                query = query.Where(u => u.cedula.Contains(filtros.cedula));
            }

            if (!string.IsNullOrWhiteSpace(filtros.departamento))
            {
                query = query.Where(u => !string.IsNullOrEmpty(u.departamento) &&
                u.departamento.Contains(filtros.departamento, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtros.rol))
            {
                query = query.Where(u => u.Rol != null && u.Rol.nombre.Equals(filtros.rol, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtros.estado))
            {
                bool estadoBool = filtros.estado.Equals("Activo", StringComparison.OrdinalIgnoreCase);
                query = query.Where(u => u.estado == estadoBool);
            }

            if (filtros.fechaCreacion.HasValue)
            {
                var fechaInicio = filtros.fechaCreacion.Value.Date;
                var fechaFin = fechaInicio.AddDays(1);
                query = query.Where(u => u.fechaCreacion >= fechaInicio && u.fechaCreacion < fechaFin);
            }

            return query.ToList();
        }

        private string GenerarContrasenaTemporal()
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string symbols = "!@#$%&*";

            var rnd = new Random();
            var password = new string(
                Enumerable.Repeat(upper, 3).Select(s => s[rnd.Next(s.Length)])
                .Concat(Enumerable.Repeat(lower, 3).Select(s => s[rnd.Next(s.Length)]))
                .Concat(Enumerable.Repeat(numbers, 2).Select(s => s[rnd.Next(s.Length)]))
                .Concat(Enumerable.Repeat(symbols, 2).Select(s => s[rnd.Next(s.Length)]))
                .OrderBy(x => rnd.Next())
                .ToArray());

            return password;
        }

        private string HashearContrasena(string contrasena)
        {
            return BCrypt.Net.BCrypt.HashPassword(contrasena);
        }

        public async Task<string> CambiarContrasenaAsync(string cedula, string contrasenaActual, string nuevaContrasena)
        {
            try
            {
                var usuario = await _usuarioRepository.EncontrarPorCedula(cedula);
                if (usuario == null)
                {
                    return "USUARIO_NO_ENCONTRADO";
                }

                if (!BCrypt.Net.BCrypt.Verify(contrasenaActual, usuario.contrasena))
                {
                    return "CONTRASEÑA_INCORRECTA";
                }

                if (BCrypt.Net.BCrypt.Verify(nuevaContrasena, usuario.contrasena))
                {
                    return "CONTRASEÑA_IGUAL";
                }

                usuario.contrasena = HashearContrasena(nuevaContrasena);

                await _usuarioRepository.Actualizar(usuario);
                await _usuarioRepository.GuardarCambios();

                _logger.LogInformation("Contraseña cambiada exitosamente para usuario {Cedula}", cedula);
                return "SUCCESS";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña para usuario {Cedula}", cedula);
                return "ERROR";
            }
        }


        // Métodos específicos para HU-005: Editar mi propia información
        public async Task<MiPerfilDto?> ObtenerMiPerfilAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioRepository.EncontrarPorCedula(cedula);
                if (usuario == null)
                    return null;

                return new MiPerfilDto
                {
                    Id_Usuario = usuario.Id_Usuario,
                    cedula = usuario.cedula,
                    nombre = usuario.nombre,
                    correo_electronico = usuario.correo_electronico,
                    departamento = usuario.departamento,
                    nombreRol = usuario.Rol?.nombre ?? "Sin rol",
                    estado = usuario.estado,
                    fechaCreacion = usuario.fechaCreacion,
                    fechaUltimaModificacion = usuario.fechaUltimaModificacion,
                    ultimoAcceso = usuario.ultimoAcceso
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil del usuario {Cedula}", cedula);
                return null;
            }
        }

        public async Task<string> ActualizarMiPerfilAsync(int userId, ActualizarMiPerfilDto perfilDto)
        {
            try
            {
                // Verificar si el usuario existe
                var usuario = await _usuarioRepository.ObtenerPorId(userId);
                if (usuario == null)
                {
                    return "USUARIO_NO_ENCONTRADO";
                }

                // Verificar que el correo no esté en uso por otro usuario
                if (await _usuarioRepository.ExistePorCorreoElectronicoExceptoUsuarioAsync(perfilDto.correo_electronico, userId))
                {
                    return "CORREO_YA_EXISTE";
                }

                // Actualizar información usando método específico del repositorio
                bool actualizado = await _usuarioRepository.ActualizarMiPerfilAsync(
                    userId,
                    perfilDto.nombre.Trim(),
                    perfilDto.correo_electronico.Trim(),
                    perfilDto.departamento?.Trim()
                );

                if (actualizado)
                {
                    _logger.LogInformation("Perfil actualizado exitosamente para usuario {UserId}", userId);
                    return "SUCCESS";
                }
                else
                {
                    return "ERROR_ACTUALIZAR";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil del usuario {UserId}", userId);
                return "ERROR";
            }
        }

        public async Task<string> CambiarMiContrasenaAsync(int userId, CambiarContrasenaDto contrasenaDto)
        {
            try
            {
                var usuario = await _usuarioRepository.ObtenerPorId(userId);
                if (usuario == null)
                {
                    return "USUARIO_NO_ENCONTRADO";
                }

                // Verificar contraseña actual
                if (!BCrypt.Net.BCrypt.Verify(contrasenaDto.ContrasenaActual, usuario.contrasena))
                {
                    return "CONTRASEÑA_ACTUAL_INCORRECTA";
                }

                // Verificar que la nueva contraseña no sea igual a la actual
                if (BCrypt.Net.BCrypt.Verify(contrasenaDto.NuevaContrasena, usuario.contrasena))
                {
                    return "CONTRASEÑA_IGUAL";
                }

                // Hashear y actualizar contraseña
                usuario.contrasena = HashearContrasena(contrasenaDto.NuevaContrasena);
                usuario.fechaUltimaModificacion = DateTime.Now;

                await _usuarioRepository.Actualizar(usuario);
                await _usuarioRepository.GuardarCambios();

                _logger.LogInformation("Contraseña cambiada exitosamente para usuario {UserId}", userId);
                return "SUCCESS";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña del usuario {UserId}", userId);
                return "ERROR";
            }
        }

        /// <summary>
        /// Restablece la contraseña obligatoria del usuario (HU-009)
        /// No requiere verificación de contraseña actual
        /// Actualiza DebeRestablecerContrasena a false
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="nuevaContrasena">Nueva contraseña</param>
        /// <returns>Resultado de la operación</returns>
        public async Task<string> RestablecerContrasenaObligatoriaAsync(int userId, string nuevaContrasena)
        {
            try
            {
                var usuario = await _usuarioRepository.ObtenerPorId(userId);
                if (usuario == null)
                {
                    return "USUARIO_NO_ENCONTRADO";
                }

                // Verificar que realmente necesite cambiar la contraseña
                if (!usuario.DebeRestablecerContrasena)
                {
                    return "NO_REQUIERE_CAMBIO";
                }

                // Verificar que la nueva contraseña no sea igual a la actual
                if (BCrypt.Net.BCrypt.Verify(nuevaContrasena, usuario.contrasena))
                {
                    return "MISMA_CONTRASENA";
                }

                // Hashear nueva contraseña
                usuario.contrasena = HashearContrasena(nuevaContrasena);
                usuario.DebeRestablecerContrasena = false; // Marcar como ya cambiada
                usuario.fechaUltimaModificacion = DateTime.Now;

                await _usuarioRepository.Actualizar(usuario);
                await _usuarioRepository.GuardarCambios();

                _logger.LogInformation("Contraseña obligatoria restablecida exitosamente para usuario {UserId}", userId);
                return "SUCCESS";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña obligatoria del usuario {UserId}", userId);
                return "ERROR";
            }
        }

        /// <summary>
        /// Restablece la contraseña obligatoria del usuario usando cédula (HU-009)
        /// No requiere verificación de contraseña actual
        /// Actualiza DebeRestablecerContrasena a false
        /// </summary>
        /// <param name="cedula">Cédula del usuario</param>
        /// <param name="nuevaContrasena">Nueva contraseña</param>
        /// <returns>Resultado de la operación</returns>
        public async Task<string> RestablecerContrasenaObligatoriaPorCedulaAsync(string cedula, string nuevaContrasena)
        {
            try
            {
                var usuario = await _usuarioRepository.EncontrarPorCedula(cedula);
                if (usuario == null)
                {
                    return "USUARIO_NO_ENCONTRADO";
                }

                // Verificar que realmente necesite cambiar la contraseña
                if (!usuario.DebeRestablecerContrasena)
                {
                    return "NO_REQUIERE_CAMBIO";
                }

                // Verificar que la nueva contraseña no sea igual a la actual
                if (BCrypt.Net.BCrypt.Verify(nuevaContrasena, usuario.contrasena))
                {
                    return "MISMA_CONTRASENA";
                }

                // Hashear nueva contraseña
                usuario.contrasena = HashearContrasena(nuevaContrasena);
                usuario.DebeRestablecerContrasena = false; // Marcar como ya cambiada
                usuario.fechaUltimaModificacion = DateTime.Now;

                await _usuarioRepository.Actualizar(usuario);
                await _usuarioRepository.GuardarCambios();

                _logger.LogInformation("Contraseña obligatoria restablecida exitosamente para usuario {Cedula}", cedula);
                return "SUCCESS";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña obligatoria del usuario {Cedula}", cedula);
                return "ERROR";
            }
        }

        public async Task<bool> VerificarUsuarioPorDefecto()
        {

            await _rolService.validarRolesExistentes();

            var rolAdmin = await _rolService.ObtenerRolPorNombre("SUPERADMIN");
            if (rolAdmin == null)
            {
                return false;
            }

            var usuariosAdminExistentes = await _usuarioRepository.EncontrarPorIdRol(rolAdmin.idRol);

            var usuariosAdminActivos = usuariosAdminExistentes.Where(u => u.estado).ToList();

            if (!usuariosAdminActivos.Any())
            {
                var usuarioExistente = await _usuarioRepository.EncontrarPorCedula("000000000");
                if (usuarioExistente == null)
                {
                    await CrearUsuarioInicialAsync();
                    _logger.LogInformation("Usuario por defecto creado exitosamente.");
                }
                else
                {
                    // Reactivar usuario por defecto si existe pero está inactivo
                    usuarioExistente.estado = true;
                    usuarioExistente.fechaUltimaModificacion = DateTime.Now;

                    await _usuarioRepository.Actualizar(usuarioExistente);
                    await _usuarioRepository.GuardarCambios();

                }
            }

            return true;
        }

        public async Task<UsuarioInicioSesion?> obtenerDatosInicioSesion(string cedula)
        {
            try
            {
                var usuario = await ObtenerUsuarioCompletoAsync(cedula);
                if (usuario == null) return null;

                return new UsuarioInicioSesion
                {
                    cedula = usuario.cedula,
                    correo_electronico = usuario.correo_electronico,
                    contrasena = usuario.contrasena,
                    estado = usuario.estado ? "Activo" : "Inactivo",
                    intentosLoginFallidos = usuario.intentosLoginFallidos,
                    fechaBloqueado = usuario.fechaBloqueado,
                    debeRestablecerContrasena = usuario.DebeRestablecerContrasena
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de inicio de sesión por cédula: {Cedula}", cedula);
                throw;
            }
        }


        public async Task<bool> EstaActivo(string cedula)
        {
            try
            {
                var usuario = await _usuarioRepository.EncontrarPorCedula(cedula);

                if (usuario == null)
                {
                    throw new InvalidOperationException($"Usuario con cédula {cedula} no encontrado");
                }

                return usuario.estado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar si usuario está activo: {Cedula}", cedula);
                throw;
            }
        }

        public async Task<bool> EstaBloqueado(string cedula)
        {
            try
            {
                var usuario = await _usuarioRepository.EncontrarPorCedula(cedula);

                // Si no existe, lanzar excepción
                if (usuario == null)
                {
                    throw new InvalidOperationException($"Usuario con cédula {cedula} no encontrado");
                }

                if (usuario.fechaBloqueado.HasValue) // bloqueado si tiene valor
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar si usuario está bloqueado: {Cedula}", cedula);
                throw;
            }
        }

        public async Task<bool> EstablecerTwoFactorAsync(string cedula, bool habilitar)
        {
            try
            {
                var usuario = await _usuarioRepository.EncontrarPorCedula(cedula);
                if (usuario == null) return false;

                usuario.TwoFactorEnabled = habilitar;
                await _usuarioRepository.Actualizar(usuario);
                await _usuarioRepository.GuardarCambios();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estableciendo 2FA para cédula {Cedula}", cedula);
                return false;
            }
        }

        public async Task<bool> EstaHabilitadoTwoFactorAsync(string cedula)
        {
            try
            {
                var usuario = await _usuarioRepository.EncontrarPorCedula(cedula);
                return usuario != null && usuario.TwoFactorEnabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando estado de 2FA para cédula {Cedula}", cedula);
                return false;
            }
        }

        private async Task RegistrarAuditoriaSafeAsync(
            string tipoEvento,
            string descripcion,
            string modulo,
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
                    usuarioId: null,
                    datosAnteriores: datosAnteriores,
                    datosNuevos: datosNuevos);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo registrar auditoría de usuario. TipoEvento={TipoEvento}",
                    tipoEvento);
            }
        }

    }
}
