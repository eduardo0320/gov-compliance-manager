using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using backend.Controllers;
using backend.Services.Interfaces;
using backend.DTOs;
using backend.Models;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class UsuariosControllerTests
    {
        private readonly Mock<IUsuarioService> _mockUsuarioService;
        private readonly Mock<IRolService> _mockRolService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<UsuariosController>> _mockLogger;
        private readonly UsuariosController _controller;

        public UsuariosControllerTests()
        {
            _mockUsuarioService = new Mock<IUsuarioService>();
            _mockRolService = new Mock<IRolService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<UsuariosController>>();

            _controller = new UsuariosController(
                _mockUsuarioService.Object,
                _mockRolService.Object,
                _mockConfiguration.Object,
                _mockEmailService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task ListarUsuarios_DebeRetornarOkConListaDeUsuarios()
        {
            // Arrange
            var usuarios = new List<object>
            {
                new { Id_Usuario = 1, cedula = "123456789", nombre = "Juan P�rez" },
                new { Id_Usuario = 2, cedula = "987654321", nombre = "Ana L�pez" }
            };

            _mockUsuarioService.Setup(s => s.ObtenerTodosLosUsuariosAsync())
                .ReturnsAsync(usuarios);

            // Act
            var result = await _controller.ListarUsuarios();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task ListarUsuarios_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockUsuarioService.Setup(s => s.ObtenerTodosLosUsuariosAsync())
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.ListarUsuarios();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task FiltrarUsuarios_ConFiltrosValidos_DebeRetornarOkConUsuariosFiltrados()
        {
            // Arrange
            var filtros = new FiltroUsuariosDto
            {
                nombre = "Juan",
                rol = "ADMIN"
            };

            var usuariosFiltrados = new List<object>
            {
                new { Id_Usuario = 1, nombre = "Juan P�rez", rol = "ADMIN" }
            };

            _mockUsuarioService.Setup(s => s.FiltrarUsuariosAsync(filtros))
                .ReturnsAsync(usuariosFiltrados);

            // Act
            var result = await _controller.FiltrarUsuarios(filtros);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task FiltrarUsuarios_SinFiltros_DebeRetornarBadRequest()
        {
            // Act
            var result = await _controller.FiltrarUsuarios(null!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ObtenerUsuarioPorCedula_CuandoExiste_DebeRetornarOkConUsuario()
        {
            // Arrange
            var usuario = new { Id_Usuario = 1, cedula = "123456789", nombre = "Juan P�rez" };

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioPorCedulaAsync("123456789"))
                .ReturnsAsync(usuario);

            // Act
            var result = await _controller.ObtenerUsuarioPorCedula("123456789");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ObtenerUsuarioPorCedula_CuandoNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            _mockUsuarioService.Setup(s => s.ObtenerUsuarioPorCedulaAsync("999999999"))
                .ReturnsAsync((object)null!);

            // Act
            var result = await _controller.ObtenerUsuarioPorCedula("999999999");

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ObtenerUsuarioPorCedula_ConCedulaVacia_DebeRetornarBadRequest()
        {
            // Act
            var result = await _controller.ObtenerUsuarioPorCedula("");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CambiarEstadoUsuario_DebeRetornarOk()
        {
            // Arrange
            _mockUsuarioService.Setup(s => s.CambiarEstadoUsuarioAsync("123456789"))
                .ReturnsAsync("Usuario activado exitosamente");

            var claims = new List<Claim>
            {
                new Claim("rol", "ADMIN"),
                new Claim(ClaimTypes.Name, "admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CambiarEstadoUsuario("123456789");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RegistrarUsuario_ConDatosValidos_DebeRetornarOk()
        {
            // Arrange
            var dto = new UsuarioRegistroDto
            {
                cedula = "123456789",
                nombre = "Juan P�rez",
                correo_electronico = "juan@example.com",
                departamento = "TI",
                idRol = 2
            };

            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCedulaAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCorreoAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockUsuarioService.Setup(s => s.CrearUsuarioAsync(dto))
                .ReturnsAsync("Password123");

            _mockEmailService.Setup(s => s.EnviarCorreoRegistro(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var claims = new List<Claim> { new Claim("rol", "ADMIN") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.RegistrarUsuario(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RegistrarUsuario_CuandoCedulaYaExiste_DebeRetornarBadRequest()
        {
            // Arrange
            var dto = new UsuarioRegistroDto
            {
                cedula = "123456789",
                correo_electronico = "juan@example.com"
            };

            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCedulaAsync("123456789"))
                .ReturnsAsync(true);

            var claims = new List<Claim> { new Claim("rol", "ADMIN") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.RegistrarUsuario(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RegistrarUsuario_SinPermisos_DebeRetornarForbid()
        {
            // Arrange
            var dto = new UsuarioRegistroDto
            {
                cedula = "123456789",
                correo_electronico = "juan@example.com"
            };

            var claims = new List<Claim> { new Claim("rol", "USER") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.RegistrarUsuario(dto);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task ObtenerMiPerfil_ConUsuarioAutenticado_DebeRetornarOk()
        {
            // Arrange
            var perfil = new MiPerfilDto
            {
                Id_Usuario = 1,
                cedula = "123456789",
                nombre = "Juan P�rez",
                correo_electronico = "juan@example.com",
                departamento = "TI",
                nombreRol = "ADMIN",
                estado = true,
                fechaCreacion = DateTime.Now,
                fechaUltimaModificacion = DateTime.Now
            };

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioPorCedulaAsync("123456789"))
                .ReturnsAsync(perfil);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "123456789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.ObtenerMiPerfil();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ActualizarMiPerfil_ConDatosValidos_DebeRetornarOk()
        {
            // Arrange
            var perfilDto = new ActualizarMiPerfilDto
            {
                nombre = "Juan P�rez Actualizado",
                correo_electronico = "juan.nuevo@example.com",
                departamento = "TI"
            };

            _mockUsuarioService.Setup(s => s.ActualizarMiPerfilAsync(1, perfilDto))
                .ReturnsAsync("SUCCESS");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.ActualizarMiPerfil(perfilDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CambiarMiContrasena_ConDatosValidos_DebeRetornarOk()
        {
            // Arrange
            var contrasenaDto = new CambiarContrasenaDto
            {
                ContrasenaActual = "OldPassword123!",
                NuevaContrasena = "NewPassword123!",
                ConfirmarContrasena = "NewPassword123!"
            };

            _mockUsuarioService.Setup(s => s.CambiarMiContrasenaAsync(1, contrasenaDto))
                .ReturnsAsync("SUCCESS");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CambiarMiContrasena(contrasenaDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CambiarMiContrasena_ConContrasenaActualIncorrecta_DebeRetornarBadRequest()
        {
            // Arrange
            var contrasenaDto = new CambiarContrasenaDto
            {
                ContrasenaActual = "WrongPassword",
                NuevaContrasena = "NewPassword123!",
                ConfirmarContrasena = "NewPassword123!"
            };

            _mockUsuarioService.Setup(s => s.CambiarMiContrasenaAsync(1, contrasenaDto))
                .ReturnsAsync("CONTRASEÑA_ACTUAL_INCORRECTA");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CambiarMiContrasena(contrasenaDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EditarInformacion_ConDatosValidos_DebeRetornarOk()
        {
            // Arrange
            var dto = new UsuarioEdicionDto
            {
                cedula = "123456789",
                nombre = "Juan Actualizado",
                correo_electronico = "juan@example.com",
                departamento = "TI",
                idRol = 2
            };

            var usuarioExistente = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                nombre = "Juan",
                correo_electronico = "juan@example.com",
                departamento = "TI",
                idRol = 2,
                contrasena = "hash123",
                estado = true
            };

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789"))
                .ReturnsAsync(usuarioExistente);

            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCedulaAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockUsuarioService.Setup(s => s.ActualizarUsuarioAsync(It.IsAny<string>(), It.IsAny<Usuario>()))
                .ReturnsAsync(true);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.EditarInformacionAsync("123456789", dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task EditarInformacion_CuandoUsuarioNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            var dto = new UsuarioEdicionDto
            {
                cedula = "999999999",
                nombre = "No Existe",
                correo_electronico = "noexiste@example.com",
                departamento = "TI",
                idRol = 2
            };

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("999999999"))
                .ReturnsAsync((Usuario?)null);

            // Act
            var result = await _controller.EditarInformacionAsync("999999999", dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task RestablecerContrasenaObligatoria_ConDatosValidos_DebeRetornarOk()
        {
            // Arrange
            var nuevaContrasena = "NewPassword123!";

            _mockUsuarioService.Setup(s => s.RestablecerContrasenaObligatoriaPorCedulaAsync("123456789", nuevaContrasena))
                .ReturnsAsync("SUCCESS");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "123456789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.RestablecerContrasenaObligatoria(nuevaContrasena);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RestablecerContrasenaObligatoria_ConContrasenaInvalida_DebeRetornarServerError()
        {
            // Arrange
            var nuevaContrasena = "NewPassword123!";

            _mockUsuarioService.Setup(s => s.RestablecerContrasenaObligatoriaPorCedulaAsync("123456789", nuevaContrasena))
                .ReturnsAsync("CONTRASE�A_INVALIDA");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "123456789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.RestablecerContrasenaObligatoria(nuevaContrasena);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CambiarEstadoUsuario_CuandoUsuarioNoExiste_DebeRetornarBadRequest()
        {
            // Arrange
            _mockUsuarioService.Setup(s => s.CambiarEstadoUsuarioAsync("999999999"))
                .ReturnsAsync("No se encontró el usuario");

            var claims = new List<Claim>
            {
                new Claim("rol", "ADMIN"),
                new Claim(ClaimTypes.Name, "admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CambiarEstadoUsuario("999999999");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CambiarEstadoUsuario_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockUsuarioService.Setup(s => s.CambiarEstadoUsuarioAsync("123456789"))
                .ThrowsAsync(new Exception("Error de base de datos"));

            var claims = new List<Claim>
            {
                new Claim("rol", "ADMIN"),
                new Claim(ClaimTypes.Name, "admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CambiarEstadoUsuario("123456789");

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task RegistrarUsuario_CuandoCorreoYaExiste_DebeRetornarBadRequest()
        {
            // Arrange
            var dto = new UsuarioRegistroDto
            {
                cedula = "123456789",
                correo_electronico = "existente@example.com"
            };

            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCedulaAsync("123456789"))
                .ReturnsAsync(false);

            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCorreoAsync("existente@example.com"))
                .ReturnsAsync(true);

            var claims = new List<Claim> { new Claim("rol", "ADMIN") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.RegistrarUsuario(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RegistrarUsuario_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            var dto = new UsuarioRegistroDto
            {
                cedula = "123456789",
                correo_electronico = "nuevo@example.com",
                nombre = "Test",
                departamento = "TI",
                idRol = 2
            };

            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCedulaAsync("123456789"))
                .ReturnsAsync(false);

            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCorreoAsync("nuevo@example.com"))
                .ReturnsAsync(false);

            _mockUsuarioService.Setup(s => s.CrearUsuarioAsync(dto))
                .ThrowsAsync(new Exception("Error de base de datos"));

            var claims = new List<Claim> { new Claim("rol", "ADMIN") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.RegistrarUsuario(dto);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ActualizarMiPerfil_CuandoCorreoYaExiste_DebeRetornarBadRequest()
        {
            // Arrange
            var perfilDto = new ActualizarMiPerfilDto
            {
                nombre = "Juan P�rez",
                correo_electronico = "existente@example.com",
                departamento = "TI"
            };

            _mockUsuarioService.Setup(s => s.ActualizarMiPerfilAsync(1, perfilDto))
                .ReturnsAsync("CORREO_YA_EXISTE");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.ActualizarMiPerfil(perfilDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ActualizarMiPerfil_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            var perfilDto = new ActualizarMiPerfilDto
            {
                nombre = "Juan P�rez",
                correo_electronico = "juan@example.com",
                departamento = "TI"
            };

            _mockUsuarioService.Setup(s => s.ActualizarMiPerfilAsync(1, perfilDto))
                .ThrowsAsync(new Exception("Error de base de datos"));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.ActualizarMiPerfil(perfilDto);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CambiarMiContrasena_ConContrasenasNoCoinciden_DebeRetornarServerError()
        {
            // Arrange
            var contrasenaDto = new CambiarContrasenaDto
            {
                ContrasenaActual = "OldPassword123!",
                NuevaContrasena = "NewPassword123!",
                ConfirmarContrasena = "DifferentPassword123!"
            };

            _mockUsuarioService.Setup(s => s.CambiarMiContrasenaAsync(1, contrasenaDto))
                .ReturnsAsync("LAS_CONTRASE�AS_NO_COINCIDEN");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CambiarMiContrasena(contrasenaDto);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CambiarMiContrasena_ConContrasenaInvalida_DebeRetornarServerError()
        {
            // Arrange
            var contrasenaDto = new CambiarContrasenaDto
            {
                ContrasenaActual = "OldPassword123!",
                NuevaContrasena = "weak",
                ConfirmarContrasena = "weak"
            };

            _mockUsuarioService.Setup(s => s.CambiarMiContrasenaAsync(1, contrasenaDto))
                .ReturnsAsync("CONTRASE�A_INVALIDA");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CambiarMiContrasena(contrasenaDto);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CambiarMiContrasena_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            var contrasenaDto = new CambiarContrasenaDto
            {
                ContrasenaActual = "OldPassword123!",
                NuevaContrasena = "NewPassword123!",
                ConfirmarContrasena = "NewPassword123!"
            };

            _mockUsuarioService.Setup(s => s.CambiarMiContrasenaAsync(1, contrasenaDto))
                .ThrowsAsync(new Exception("Error de base de datos"));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CambiarMiContrasena(contrasenaDto);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task EditarInformacion_CuandoCedulaYaExiste_DebeRetornarBadRequest()
        {
            // Arrange
            var dto = new UsuarioEdicionDto
            {
                cedula = "987654321",
                nombre = "Juan",
                correo_electronico = "juan@example.com",
                departamento = "TI",
                idRol = 2
            };

            var usuarioExistente = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                nombre = "Juan",
                correo_electronico = "juan@example.com",
                departamento = "TI",
                idRol = 2,
                contrasena = "hash123",
                estado = true
            };

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789"))
                .ReturnsAsync(usuarioExistente);

            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCedulaAsync("987654321"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.EditarInformacionAsync("123456789", dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EditarInformacion_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            var dto = new UsuarioEdicionDto
            {
                cedula = "123456789",
                nombre = "Juan",
                correo_electronico = "juan@example.com",
                departamento = "TI",
                idRol = 2
            };

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789"))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.EditarInformacionAsync("123456789", dto);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task RestablecerContrasenaObligatoria_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            var nuevaContrasena = "NewPassword123!";

            _mockUsuarioService.Setup(s => s.RestablecerContrasenaObligatoriaPorCedulaAsync("123456789", nuevaContrasena))
                .ThrowsAsync(new Exception("Error de base de datos"));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "123456789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.RestablecerContrasenaObligatoria(nuevaContrasena);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerMiPerfil_CuandoPerfilNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            _mockUsuarioService.Setup(s => s.ObtenerUsuarioPorCedulaAsync("123456789"))
                .ReturnsAsync((MiPerfilDto?)null);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "123456789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.ObtenerMiPerfil();

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ObtenerMiPerfil_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockUsuarioService.Setup(s => s.ObtenerUsuarioPorCedulaAsync("123456789"))
                .ThrowsAsync(new Exception("Error de base de datos"));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "123456789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.ObtenerMiPerfil();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task FiltrarUsuarios_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            var filtros = new FiltroUsuariosDto
            {
                nombre = "Juan"
            };

            _mockUsuarioService.Setup(s => s.FiltrarUsuariosAsync(filtros))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.FiltrarUsuarios(filtros);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerUsuarioPorCedula_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockUsuarioService.Setup(s => s.ObtenerUsuarioPorCedulaAsync("123456789"))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.ObtenerUsuarioPorCedula("123456789");

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }
    }
}
