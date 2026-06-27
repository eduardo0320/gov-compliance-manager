using Xunit;
using Moq;
using FluentAssertions;
using backend.Models;
using backend.Services.Implementations;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using backend.DTOs;

namespace backend.Tests.Services
{
    public class UsuarioServiceTests
    {
        private readonly Mock<IUsuarioRepository> _mockUsuarioRepo;
        private readonly Mock<IRolService> _mockRolService;
        private readonly Mock<ILogger<UsuarioService>> _mockLogger;
        private readonly UsuarioService _service;

        public UsuarioServiceTests()
        {
            _mockUsuarioRepo = new Mock<IUsuarioRepository>();
            _mockRolService = new Mock<IRolService>();
            _mockLogger = new Mock<ILogger<UsuarioService>>();

            _service = new UsuarioService(
                _mockUsuarioRepo.Object,
                _mockRolService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ObtenerTodosLosUsuariosAsync_DebeRetornarUsuarios()
        {
            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "111", nombre = "Usuario 1", idRol = 1 },
                new Usuario { Id_Usuario = 2, cedula = "222", nombre = "Usuario 2", idRol = 2 }
            };

            _mockUsuarioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(usuarios);

            var resultado = await _service.ObtenerTodosLosUsuariosAsync();

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerUsuarioPorCedulaAsync_ConCedulaValida_DebeRetornarUsuario()
        {
            var usuario = new Usuario { Id_Usuario = 1, cedula = "123456789", nombre = "Test", idRol = 1 };
            _mockUsuarioRepo.Setup(r => r.EncontrarPorCedula("123456789")).ReturnsAsync(usuario);

            var resultado = await _service.ObtenerUsuarioPorCedulaAsync("123456789");

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerUsuarioCompletoAsync_ConCedulaValida_DebeRetornarEntidad()
        {
            var usuario = new Usuario { Id_Usuario = 1, cedula = "123456789", nombre = "Test", idRol = 1 };
            _mockUsuarioRepo.Setup(r => r.EncontrarPorCedula("123456789")).ReturnsAsync(usuario);

            var resultado = await _service.ObtenerUsuarioCompletoAsync("123456789");

            resultado.Should().BeSameAs(usuario);
        }

        [Fact]
        public async Task ObtenerUsuarioPorIdAsync_ConIdValido_DebeRetornarUsuario()
        {
            var usuario = new Usuario { Id_Usuario = 1, cedula = "111", nombre = "Usuario", idRol = 1 };
            _mockUsuarioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(usuario);

            var resultado = await _service.ObtenerUsuarioPorIdAsync(1);

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task CrearUsuarioAsync_ConDatosValidos_DebeRetornarContrasenaTemporal()
        {
            var dto = new UsuarioRegistroDto
            {
                cedula = "123456789",
                nombre = "New User",
                correo_electronico = "new@test.com",
                departamento = "IT",
                idRol = 1
            };

            _mockUsuarioRepo.Setup(r => r.EncontrarPorCedula("123456789")).ReturnsAsync((Usuario?)null);
            _mockUsuarioRepo.Setup(r => r.ExistePorCorreoElectronico("new@test.com")).ReturnsAsync(false);
            _mockRolService.Setup(s => s.ObtenerRolPorId(1)).ReturnsAsync(new Rol { idRol = 1, nombre = "ADMIN" });
            _mockUsuarioRepo.Setup(r => r.Agregar(It.IsAny<Usuario>())).ReturnsAsync(new Usuario { Id_Usuario = 1 });
            _mockUsuarioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.CrearUsuarioAsync(dto);

            resultado.Should().NotBeNullOrWhiteSpace();
            resultado.Length.Should().BeGreaterThan(5);
        }

        [Fact]
        public async Task ActualizarUsuarioAsync_ConDatosValidos_DebeRetornarTrue()
        {
            var existente = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                nombre = "Old Name",
                correo_electronico = "old@test.com",
                idRol = 1,
                estado = true,
                contrasena = "hash"
            };

            var actualizado = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                nombre = "New Name",
                correo_electronico = "old@test.com",
                idRol = 1,
                estado = true,
                contrasena = "hash"
            };

            _mockUsuarioRepo.Setup(r => r.EncontrarPorCedula("123456789")).ReturnsAsync(existente);
            _mockRolService.Setup(s => s.ObtenerRolPorId(1)).ReturnsAsync(new Rol { idRol = 1, nombre = "ADMIN" });
            _mockUsuarioRepo.Setup(r => r.Actualizar(It.IsAny<Usuario>())).ReturnsAsync(existente);
            _mockUsuarioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarUsuarioAsync("123456789", actualizado);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task EliminarUsuarioAsync_ConIdValido_DebeRetornarTrue()
        {
            _mockUsuarioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(new Usuario { Id_Usuario = 1, cedula = "111", nombre = "U" });
            _mockUsuarioRepo.Setup(r => r.Eliminar(1)).ReturnsAsync(true);
            _mockUsuarioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.EliminarUsuarioAsync(1);

            resultado.Should().BeTrue();
            _mockUsuarioRepo.Verify(r => r.Eliminar(1), Times.Once);
        }

        [Fact]
        public async Task CambiarEstadoUsuarioAsync_ConUsuarioNormal_DebeAlternarEstado()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123",
                nombre = "Test",
                estado = true,
                idRol = 2,
                Rol = new Rol { idRol = 2, nombre = "ADMIN" }
            };

            _mockUsuarioRepo.Setup(r => r.EncontrarPorCedula("123")).ReturnsAsync(usuario);
            _mockUsuarioRepo.Setup(r => r.Actualizar(It.IsAny<Usuario>())).ReturnsAsync(usuario);
            _mockUsuarioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.CambiarEstadoUsuarioAsync("123");

            resultado.Should().Contain("exitosamente");
            usuario.estado.Should().BeFalse();
        }

        [Fact]
        public async Task FiltrarUsuariosAsync_ConFiltroNombre_DebeFiltrar()
        {
            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "123", nombre = "Test User", idRol = 1, estado = true },
                new Usuario { Id_Usuario = 2, cedula = "456", nombre = "Otro", idRol = 1, estado = true }
            };

            _mockUsuarioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(usuarios);

            var resultado = await _service.FiltrarUsuariosAsync(new FiltroUsuariosDto { nombre = "Test" });

            resultado.Should().HaveCount(1);
        }

        [Fact]
        public async Task ExisteUsuarioPorCedulaAsync_ConExistente_DebeRetornarTrue()
        {
            _mockUsuarioRepo.Setup(r => r.EncontrarPorCedula("123")).ReturnsAsync(new Usuario { Id_Usuario = 1, cedula = "123", nombre = "U" });

            var resultado = await _service.ExisteUsuarioPorCedulaAsync("123");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ExisteUsuarioPorCorreoAsync_ConExistente_DebeRetornarTrue()
        {
            _mockUsuarioRepo.Setup(r => r.ExistePorCorreoElectronico("test@test.com")).ReturnsAsync(true);

            var resultado = await _service.ExisteUsuarioPorCorreoAsync("test@test.com");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ContarSuperAdminsActivosAsync_DebeRetornarConteo()
        {
            var rol = new Rol { idRol = 1, nombre = "SUPERADMIN" };
            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "123", nombre = "Admin1", idRol = 1, estado = true },
                new Usuario { Id_Usuario = 2, cedula = "456", nombre = "Admin2", idRol = 1, estado = false },
                new Usuario { Id_Usuario = 3, cedula = "789", nombre = "Admin3", idRol = 1, estado = true }
            };

            _mockRolService.Setup(s => s.ObtenerRolPorNombre("SUPERADMIN")).ReturnsAsync(rol);
            _mockUsuarioRepo.Setup(r => r.EncontrarPorIdRol(1)).ReturnsAsync(usuarios);

            var resultado = await _service.ContarSuperAdminsActivosAsync();

            resultado.Should().Be(2);
        }

        [Fact]
        public async Task CambiarContrasenaAsync_ConContrasenaCorrecta_DebeRetornarSuccess()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123",
                nombre = "Test",
                contrasena = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
                idRol = 1
            };

            _mockUsuarioRepo.Setup(r => r.EncontrarPorCedula("123")).ReturnsAsync(usuario);
            _mockUsuarioRepo.Setup(r => r.Actualizar(It.IsAny<Usuario>())).ReturnsAsync(usuario);
            _mockUsuarioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.CambiarContrasenaAsync("123", "oldpassword", "newpassword123");

            resultado.Should().Be("SUCCESS");
        }

        [Fact]
        public async Task ObtenerMiPerfilAsync_ConCedulaValida_DebeRetornarPerfil()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123",
                nombre = "Test User",
                correo_electronico = "test@test.com",
                departamento = "IT",
                idRol = 1,
                estado = true,
                Rol = new Rol { idRol = 1, nombre = "Admin" }
            };

            _mockUsuarioRepo.Setup(r => r.EncontrarPorCedula("123")).ReturnsAsync(usuario);

            var resultado = await _service.ObtenerMiPerfilAsync("123");

            resultado.Should().NotBeNull();
            resultado!.nombre.Should().Be("Test User");
            resultado.nombreRol.Should().Be("Admin");
        }

        [Fact]
        public async Task ActualizarMiPerfilAsync_ConDatosValidos_DebeRetornarSuccess()
        {
            var perfilDto = new ActualizarMiPerfilDto
            {
                nombre = "New Name",
                correo_electronico = "new@test.com",
                departamento = "IT"
            };

            _mockUsuarioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(new Usuario { Id_Usuario = 1, cedula = "123", nombre = "Old", correo_electronico = "old@test.com", idRol = 1 });
            _mockUsuarioRepo.Setup(r => r.ExistePorCorreoElectronicoExceptoUsuarioAsync("new@test.com", 1)).ReturnsAsync(false);
            _mockUsuarioRepo.Setup(r => r.ActualizarMiPerfilAsync(1, "New Name", "new@test.com", "IT")).ReturnsAsync(true);

            var resultado = await _service.ActualizarMiPerfilAsync(1, perfilDto);

            resultado.Should().Be("SUCCESS");
        }

        [Fact]
        public async Task CambiarMiContrasenaAsync_ConContrasenaCorrecta_DebeRetornarSuccess()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123",
                nombre = "Test",
                contrasena = BCrypt.Net.BCrypt.HashPassword("currentpass"),
                idRol = 1
            };

            var dto = new CambiarContrasenaDto
            {
                ContrasenaActual = "currentpass",
                NuevaContrasena = "newpass123"
            };

            _mockUsuarioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(usuario);
            _mockUsuarioRepo.Setup(r => r.Actualizar(It.IsAny<Usuario>())).ReturnsAsync(usuario);
            _mockUsuarioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.CambiarMiContrasenaAsync(1, dto);

            resultado.Should().Be("SUCCESS");
        }

        [Fact]
        public async Task RestablecerContrasenaObligatoriaPorCedulaAsync_ConUsuarioValido_DebeRetornarSuccess()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123",
                nombre = "Test",
                contrasena = BCrypt.Net.BCrypt.HashPassword("currentpass"),
                idRol = 1,
                DebeRestablecerContrasena = true
            };

            _mockUsuarioRepo.Setup(r => r.EncontrarPorCedula("123")).ReturnsAsync(usuario);
            _mockUsuarioRepo.Setup(r => r.Actualizar(It.IsAny<Usuario>())).ReturnsAsync(usuario);
            _mockUsuarioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.RestablecerContrasenaObligatoriaPorCedulaAsync("123", "newpass123!");

            resultado.Should().Be("SUCCESS");
            usuario.DebeRestablecerContrasena.Should().BeFalse();
        }
    }
}
