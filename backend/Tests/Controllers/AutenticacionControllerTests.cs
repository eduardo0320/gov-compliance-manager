using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using backend.Controladores;
using backend.Services.Interfaces;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class AutenticacionControllerTests
    {
        private readonly Mock<IAutenticacionService> _mockAutenticacionService;
        private readonly IWebHostEnvironment _mockEnv;
        private readonly AutenticacionController _controller;

        public AutenticacionControllerTests()
        {
            _mockAutenticacionService = new Mock<IAutenticacionService>();
            _mockEnv = Mock.Of<IWebHostEnvironment>();
            _controller = new AutenticacionController(_mockAutenticacionService.Object, _mockEnv);
        }

        // [Fact]
        // public async Task IniciarSesion_ConCredencialesValidas_DebeRetornarOkConToken()
        // {
        //     // Arrange
        //     var solicitud = new SolicitudInicioSesion
        //     {
        //         cedula = "123456789",
        //         contrasena = "Password123"
        //     };

        //     var usuario = new { id = 1, nombre = "Juan P�rez", rol = "ADMIN" };
        //     var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

        //     _mockAutenticacionService.Setup(s => s.IniciarSesionAsync("123456789", "Password123"))
        //         .ReturnsAsync((true, "Inicio de sesi�n exitoso", usuario, token));

        //     // Act
        //     var result = await _controller.IniciarSesion(solicitud);

        //     // Assert
        //     result.Should().BeOfType<OkObjectResult>();
        //     var okResult = result as OkObjectResult;
        //     okResult!.Value.Should().NotBeNull();
        // }

        // [Fact]
        // public async Task IniciarSesion_ConCredencialesInvalidas_DebeRetornarUnauthorized()
        // {
        //     // Arrange
        //     var solicitud = new SolicitudInicioSesion
        //     {
        //         cedula = "123456789",
        //         contrasena = "WrongPassword"
        //     };

        //     _mockAutenticacionService.Setup(s => s.IniciarSesionAsync("123456789", "WrongPassword"))
        //         .ReturnsAsync((false, "Credenciales incorrectas", null, null));

        //     // Act
        //     var result = await _controller.IniciarSesion(solicitud);

        //     // Assert
        //     result.Should().BeOfType<UnauthorizedObjectResult>();
        // }

        // [Fact]
        // public async Task IniciarSesion_CuandoOcurreError_DebeRetornarServerError()
        // {
        //     // Arrange
        //     var solicitud = new SolicitudInicioSesion
        //     {
        //         cedula = "123456789",
        //         contrasena = "Password123"
        //     };

        //     _mockAutenticacionService.Setup(s => s.IniciarSesionAsync(It.IsAny<string>(), It.IsAny<string>()))
        //         .ThrowsAsync(new Exception("Error de base de datos"));

        //     // Act
        //     var result = await _controller.IniciarSesion(solicitud);

        //     // Assert
        //     var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        //     statusCodeResult.StatusCode.Should().Be(500);
        // }

        [Fact]
        public async Task Registrar_ConDatosValidos_DebeRetornarOk()
        {
            // Arrange
            var solicitud = new SolicitudRegistro
            {
                cedula = "123456789",
                contrasena = "Password123",
                nombre = "Juan P�rez",
                correo_electronico = "juan@example.com",
                departamento = "TI",
                idRol = 2
            };

            _mockAutenticacionService.Setup(s => s.RegistrarUsuarioAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((true, "Usuario registrado exitosamente"));

            // Act
            var result = await _controller.Registrar(solicitud);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Registrar_CuandoUsuarioYaExiste_DebeRetornarConflict()
        {
            // Arrange
            var solicitud = new SolicitudRegistro
            {
                cedula = "123456789",
                contrasena = "Password123",
                nombre = "Juan P�rez",
                correo_electronico = "juan@example.com"
            };

            _mockAutenticacionService.Setup(s => s.RegistrarUsuarioAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((false, "Usuario ya existe"));

            // Act
            var result = await _controller.Registrar(solicitud);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task Registrar_ConDatosInvalidos_DebeRetornarBadRequest()
        {
            // Arrange
            var solicitud = new SolicitudRegistro
            {
                cedula = "123456789",
                contrasena = "weak"
            };

            _mockAutenticacionService.Setup(s => s.RegistrarUsuarioAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((false, "Contrase�a muy d�bil"));

            // Act
            var result = await _controller.Registrar(solicitud);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void ObtenerRol_ConUsuarioAutenticado_DebeRetornarOkConRol()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Juan P�rez"),
                new Claim("rol", "ADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = _controller.ObtenerRol();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public void ObtenerRol_SinRolEnClaims_DebeRetornarUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Juan P�rez")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = _controller.ObtenerRol();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public void ObtenerVencimientoToken_ConTokenValido_DebeRetornarOkConVencimiento()
        {
            // Arrange
            var solicitud = new SolicitudVencimiento
            {
                token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
            };

            var vencimiento = DateTime.Now.AddHours(1).ToString();

            _mockAutenticacionService.Setup(s => s.ObtenerVencimientoToken(It.IsAny<string>()))
                .Returns(vencimiento);

            // Act
            var result = _controller.ObtenerVencimientoToken(solicitud);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void ObtenerVencimientoToken_SinToken_DebeRetornarBadRequest()
        {
            // Arrange
            var solicitud = new SolicitudVencimiento
            {
                token = ""
            };

            // Act
            var result = _controller.ObtenerVencimientoToken(solicitud);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task BloquearUsuario_DebeRetornarOk()
        {
            // Arrange
            _mockAutenticacionService.Setup(s => s.BloquearUsuarioAsync("123456789"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.BloquearUsuario("123456789");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockAutenticacionService.Verify(s => s.BloquearUsuarioAsync("123456789"), Times.Once);
        }

        [Fact]
        public async Task BloquearUsuario_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockAutenticacionService.Setup(s => s.BloquearUsuarioAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Error al bloquear"));

            // Act
            var result = await _controller.BloquearUsuario("123456789");

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DesbloquearUsuario_DebeRetornarOk()
        {
            // Arrange
            _mockAutenticacionService.Setup(s => s.DesbloquearUsuarioAsync("123456789"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DesbloquearUsuario("123456789");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockAutenticacionService.Verify(s => s.DesbloquearUsuarioAsync("123456789"), Times.Once);
        }

        [Fact]
        public async Task CambiarContrasena_ConDatosValidos_DebeRetornarOk()
        {
            // Arrange
            var solicitud = new SolicitudCambioContrasena
            {
                contrasenaActual = "OldPassword123",
                nuevaContrasena = "NewPassword123"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "123456789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            _mockAutenticacionService.Setup(s => s.CambiarContrasenaAsync("123456789", "OldPassword123", "NewPassword123"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CambiarContrasena(solicitud);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CambiarContrasena_ConContrasenaActualIncorrecta_DebeRetornarBadRequest()
        {
            // Arrange
            var solicitud = new SolicitudCambioContrasena
            {
                contrasenaActual = "WrongPassword",
                nuevaContrasena = "NewPassword123"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "123456789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            _mockAutenticacionService.Setup(s => s.CambiarContrasenaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CambiarContrasena(solicitud);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task SolicitarRecuperacion_ConDatosValidos_DebeRetornarOk()
        {
            // Arrange
            var solicitud = new SolicitudRecuperacion
            {
                cedula = "123456789",
            };

            _mockAutenticacionService.Setup(s => s.SolicitarCodigoRecuperacionAsync("123456789"))
                .ReturnsAsync((true, "C�digo enviado exitosamente"));

            // Act
            var result = await _controller.SolicitarRecuperacion(solicitud);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task SolicitarRecuperacion_CuandoUsuarioNoExiste_DebeRetornarBadRequest()
        {
            // Arrange
            var solicitud = new SolicitudRecuperacion
            {
                cedula = "999999999",
            };

            _mockAutenticacionService.Setup(s => s.SolicitarCodigoRecuperacionAsync(It.IsAny<string>()))
                .ReturnsAsync((false, "Usuario no encontrado"));

            // Act
            var result = await _controller.SolicitarRecuperacion(solicitud);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ConfirmarRecuperacion_ConCodigoValido_DebeRetornarOk()
        {
            // Arrange
            var solicitud = new ConfirmarRecuperacion
            {
                cedula = "123456789",
                codigo = "123456",
                nuevaContrasena = "NewPassword123"
            };

            _mockAutenticacionService.Setup(s => s.ConfirmarCodigoRecuperacionAsync(
                    "123456789", "123456", "NewPassword123"))
                .ReturnsAsync((true, "Contrase�a restablecida exitosamente"));

            // Act
            var result = await _controller.ConfirmarRecuperacion(solicitud);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ConfirmarRecuperacion_ConCodigoInvalido_DebeRetornarBadRequest()
        {
            // Arrange
            var solicitud = new ConfirmarRecuperacion
            {
                cedula = "123456789",
                codigo = "wrong",
                nuevaContrasena = "NewPassword123"
            };

            _mockAutenticacionService.Setup(s => s.ConfirmarCodigoRecuperacionAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((false, "C�digo inv�lido o expirado"));

            // Act
            var result = await _controller.ConfirmarRecuperacion(solicitud);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Registrar_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            var solicitud = new SolicitudRegistro
            {
                cedula = "123456789",
                contrasena = "Password123"
            };

            _mockAutenticacionService.Setup(s => s.RegistrarUsuarioAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.Registrar(solicitud);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public void ObtenerRol_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = null!
            };

            // Act
            var result = _controller.ObtenerRol();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public void ObtenerVencimientoToken_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            var solicitud = new SolicitudVencimiento
            {
                token = "token_invalido"
            };

            _mockAutenticacionService.Setup(s => s.ObtenerVencimientoToken(It.IsAny<string>()))
                .Throws(new Exception("Token inv�lido"));

            // Act
            var result = _controller.ObtenerVencimientoToken(solicitud);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DesbloquearUsuario_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockAutenticacionService.Setup(s => s.DesbloquearUsuarioAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Error al desbloquear"));

            // Act
            var result = await _controller.DesbloquearUsuario("123456789");

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CambiarContrasena_ConUsuarioInvalido_DebeRetornarBadRequest()
        {
            // Arrange
            var solicitud = new SolicitudCambioContrasena
            {
                contrasenaActual = "OldPassword123",
                nuevaContrasena = "NewPassword123"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "invalid")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CambiarContrasena(solicitud);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CambiarContrasena_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            var solicitud = new SolicitudCambioContrasena
            {
                contrasenaActual = "OldPassword123",
                nuevaContrasena = "NewPassword123"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "123456789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            _mockAutenticacionService.Setup(s => s.CambiarContrasenaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.CambiarContrasena(solicitud);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task IniciarSesion_CuandoUsuarioEstaBloqueado_DebeRetornarUnauthorized()
        {
            // Arrange
            var solicitud = new SolicitudInicioSesion
            {
                cedula = "123456789",
                contrasena = "Password123"
            };

            _mockAutenticacionService.Setup(s => s.IniciarSesionAsync("123456789", "Password123"))
                .ReturnsAsync((false, "Usuario bloqueado", null, null));

            // Act
            var result = await _controller.IniciarSesion(solicitud);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
