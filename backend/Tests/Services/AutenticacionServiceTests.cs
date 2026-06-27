using Xunit;
using Moq;
using FluentAssertions;
using backend.Models;
using backend.Models.UsuarioContrasena;
using backend.Services.Implementations;
using backend.Services.Interfaces;
using backend.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Services
{
    public class AutenticacionServiceTests
    {
        private readonly Mock<IUsuarioService> _mockUsuarioService;
        private readonly Mock<IRolService> _mockRolService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AutenticacionService>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly NormasDb _context;
        private readonly AutenticacionService _service;

        public AutenticacionServiceTests()
        {
            _mockUsuarioService = new Mock<IUsuarioService>();
            _mockRolService = new Mock<IRolService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AutenticacionService>>();
            _mockEmailService = new Mock<IEmailService>();

            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new NormasDb(options);

            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["Key"]).Returns("ClaveSecretaSuperSeguraParaJWTQueDebeSerLargaYCompleja12345");
            jwtSection.Setup(x => x["Issuer"]).Returns("backend");
            jwtSection.Setup(x => x["Audience"]).Returns("frontend");
            jwtSection.Setup(x => x["ExpiryInMinutes"]).Returns("30");

            _mockConfiguration.Setup(c => c.GetSection("JWT")).Returns(jwtSection.Object);

            _service = new AutenticacionService(
                _mockUsuarioService.Object,
                _mockRolService.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _context
            );
        }

        [Fact]
        public async Task IniciarSesionAsync_ConCredencialesVacias_DebeRetornarError()
        {
            var resultado = await _service.IniciarSesionAsync("", "");

            resultado.exito.Should().BeFalse();
            resultado.mensaje.Should().Be("Cédula y contraseña son requeridas");
        }

        [Fact]
        public async Task IniciarSesionAsync_ConUsuarioInexistente_DebeRetornarError()
        {
            _mockUsuarioService.Setup(s => s.obtenerDatosInicioSesion("999999999"))
                .ReturnsAsync((UsuarioInicioSesion?)null);

            var resultado = await _service.IniciarSesionAsync("999999999", "Password123");

            resultado.exito.Should().BeFalse();
            resultado.mensaje.Should().Be("Credenciales inválidas");
        }

        [Fact]
        public async Task IniciarSesionAsync_ConCredencialesValidas_DebeRetornarExito()
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123");
            var usuarioLogin = new UsuarioInicioSesion
            {
                cedula = "123456789",
                contrasena = passwordHash,
                estado = "Activo",
                intentosLoginFallidos = 0,
                debeRestablecerContrasena = false
            };

            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                contrasena = passwordHash,
                idRol = 1,
                nombre = "Test User"
            };

            var rol = new Rol { idRol = 1, nombre = "Usuario" };
            var perfil = new MiPerfilDto
            {
                cedula = "123456789",
                nombre = "Test User",
                correo_electronico = "test@test.com",
                departamento = "IT",
                nombreRol = "Usuario",
                estado = true
            };

            _mockUsuarioService.Setup(s => s.obtenerDatosInicioSesion("123456789")).ReturnsAsync(usuarioLogin);
            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789")).ReturnsAsync(usuario);
            _mockUsuarioService.Setup(s => s.ObtenerMiPerfilAsync("123456789")).ReturnsAsync(perfil);
            _mockUsuarioService.Setup(s => s.ActualizarUsuarioAsync("123456789", It.IsAny<Usuario>())).ReturnsAsync(true);
            _mockRolService.Setup(s => s.ObtenerRolPorId(1)).ReturnsAsync(rol);

            var resultado = await _service.IniciarSesionAsync("123456789", "Password123");

            resultado.exito.Should().BeTrue();
            resultado.mensaje.Should().Be("Login exitoso");
            resultado.token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task IniciarSesionAsync_ConCambioObligatorio_DebeRetornarFlujoEspecial()
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123");
            var usuarioLogin = new UsuarioInicioSesion
            {
                cedula = "123456789",
                contrasena = passwordHash,
                estado = "Activo",
                debeRestablecerContrasena = true
            };

            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                contrasena = passwordHash,
                idRol = 1
            };

            _mockUsuarioService.Setup(s => s.obtenerDatosInicioSesion("123456789")).ReturnsAsync(usuarioLogin);
            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789")).ReturnsAsync(usuario);
            _mockUsuarioService.Setup(s => s.ActualizarUsuarioAsync("123456789", It.IsAny<Usuario>())).ReturnsAsync(true);
            _mockRolService.Setup(s => s.ObtenerRolPorId(1)).ReturnsAsync(new Rol { idRol = 1, nombre = "Usuario" });

            var resultado = await _service.IniciarSesionAsync("123456789", "Password123");

            resultado.exito.Should().BeTrue();
            resultado.mensaje.Should().Be("CAMBIO_CONTRASENA_REQUERIDO");
            resultado.token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task RegistrarUsuarioAsync_ConCedulaVacia_DebeRetornarError()
        {
            var resultado = await _service.RegistrarUsuarioAsync("", "Nombre", "correo@test.com", "IT", 1);

            resultado.exito.Should().BeFalse();
            resultado.mensaje.Should().Be("Cédula es requerida");
        }

        [Fact]
        public async Task RegistrarUsuarioAsync_ConDatosValidos_DebeCrearUsuario()
        {
            _mockUsuarioService.Setup(s => s.ExisteUsuarioPorCedulaAsync("123456789")).ReturnsAsync(false);
            _mockRolService.Setup(s => s.ObtenerRolPorId(1)).ReturnsAsync(new Rol { idRol = 1, nombre = "Usuario" });
            _mockUsuarioService.Setup(s => s.CrearUsuarioAsync(It.IsAny<UsuarioRegistroDto>())).ReturnsAsync("OK");

            var resultado = await _service.RegistrarUsuarioAsync("123456789", "Test User", "test@test.com", "IT", 1);

            resultado.exito.Should().BeTrue();
            resultado.mensaje.Should().Be("Usuario creado exitosamente");
            _mockUsuarioService.Verify(s => s.CrearUsuarioAsync(It.IsAny<UsuarioRegistroDto>()), Times.Once);
        }

        [Fact]
        public async Task CambiarContrasenaAsync_ConContrasenaCorrecta_DebeRetornarTrue()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                contrasena = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789")).ReturnsAsync(usuario);
            _mockUsuarioService.Setup(s => s.ActualizarUsuarioAsync("123456789", It.IsAny<Usuario>())).ReturnsAsync(true);

            var resultado = await _service.CambiarContrasenaAsync("123456789", "OldPassword123", "NewPassword123!");

            resultado.Should().BeTrue();
            BCrypt.Net.BCrypt.Verify("NewPassword123!", usuario.contrasena).Should().BeTrue();
        }

        [Fact]
        public async Task CambiarContrasenaAsync_ConContrasenaIncorrecta_DebeRetornarFalse()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                contrasena = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
            };

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789")).ReturnsAsync(usuario);

            var resultado = await _service.CambiarContrasenaAsync("123456789", "WrongPassword", "NewPassword123!");

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task EstaBloqueadoAsync_ConUsuarioInactivo_DebeRetornarTrue()
        {
            _mockUsuarioService.Setup(s => s.EstaActivo("123456789")).ReturnsAsync(false);

            var resultado = await _service.EstaBloqueadoAsync("123456789");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task BloquearUsuarioAsync_DebeMarcarUsuarioComoBloqueado()
        {
            var usuario = new Usuario { cedula = "123456789", estado = true, intentosLoginFallidos = 0 };
            Usuario? actualizado = null;

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789")).ReturnsAsync(usuario);
            _mockUsuarioService
                .Setup(s => s.ActualizarUsuarioAsync("123456789", It.IsAny<Usuario>()))
                .Callback<string, Usuario>((_, u) => actualizado = u)
                .ReturnsAsync(true);

            await _service.BloquearUsuarioAsync("123456789");

            actualizado.Should().NotBeNull();
            actualizado!.estado.Should().BeFalse();
            actualizado.fechaBloqueado.Should().NotBeNull();
        }

        [Fact]
        public async Task DesbloquearUsuarioAsync_DebeLimpiarBloqueoEIntentos()
        {
            var usuario = new Usuario { cedula = "123456789", estado = false, intentosLoginFallidos = 5, fechaBloqueado = DateTime.UtcNow };
            Usuario? actualizado = null;

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789")).ReturnsAsync(usuario);
            _mockUsuarioService
                .Setup(s => s.ActualizarUsuarioAsync("123456789", It.IsAny<Usuario>()))
                .Callback<string, Usuario>((_, u) => actualizado = u)
                .ReturnsAsync(true);

            await _service.DesbloquearUsuarioAsync("123456789");

            actualizado.Should().NotBeNull();
            actualizado!.estado.Should().BeTrue();
            actualizado.intentosLoginFallidos.Should().Be(0);
            actualizado.fechaBloqueado.Should().BeNull();
        }

        [Fact]
        public async Task ValidarContrasenaAsync_ConHashValido_DebeRetornarTrue()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("Password123");

            var resultado = await _service.ValidarContrasenaAsync(hash, "Password123");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task SolicitarCodigoRecuperacionAsync_ConUsuarioValido_DebeGenerarCodigo()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                nombre = "Test User",
                correo_electronico = "test@test.com",
                estado = true
            };

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789")).ReturnsAsync(usuario);
            _mockEmailService
                .Setup(s => s.EnviarCodigoRecuperacion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var resultado = await _service.SolicitarCodigoRecuperacionAsync("123456789");

            resultado.exito.Should().BeTrue();
            _context.RecuperacionesContrasena.Count().Should().Be(1);
        }

        [Fact]
        public async Task ConfirmarCodigoRecuperacionAsync_ConCodigoValido_DebeCambiarContrasena()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                correo_electronico = "test@test.com",
                contrasena = BCrypt.Net.BCrypt.HashPassword("OldPass123!"),
                estado = true
            };

            var recuperacion = new RecuperacionContrasena
            {
                UsuarioId = 1,
                CodigoHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                ExpiraEn = DateTime.UtcNow.AddMinutes(15),
                Usado = false
            };

            _context.RecuperacionesContrasena.Add(recuperacion);
            await _context.SaveChangesAsync();

            _mockUsuarioService.Setup(s => s.ObtenerUsuarioCompletoAsync("123456789")).ReturnsAsync(usuario);

            var resultado = await _service.ConfirmarCodigoRecuperacionAsync("123456789", "123456", "NewPass123!");

            resultado.exito.Should().BeTrue();
            var recGuardado = await _context.RecuperacionesContrasena.FirstAsync();
            recGuardado.Usado.Should().BeTrue();
            BCrypt.Net.BCrypt.Verify("NewPass123!", usuario.contrasena).Should().BeTrue();
        }
    }
}
