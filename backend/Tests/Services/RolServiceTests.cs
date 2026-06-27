using Xunit;
using Moq;
using FluentAssertions;
using backend.Models;
using backend.Services.Implementations;
using backend.Repositories.Interfaces;

namespace backend.Tests.Services
{
    public class RolServiceTests
    {
        private readonly Mock<IRolRepository> _mockRolRepo;
        private readonly RolService _service;

        public RolServiceTests()
        {
            _mockRolRepo = new Mock<IRolRepository>();
            _service = new RolService(_mockRolRepo.Object);
        }

        [Fact]
        public async Task ObtenerTodosLosRoles_DebeRetornarTodosLosRoles()
        {
            var roles = new List<Rol>
            {
                new Rol { idRol = 1, nombre = "Admin" },
                new Rol { idRol = 2, nombre = "Usuario" }
            };

            _mockRolRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(roles);

            var resultado = await _service.ObtenerTodosLosRoles();

            resultado.Should().NotBeNull();
            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task CrearRolAsync_ConRolValido_DebeCrearRol()
        {
            var nuevoRol = new Rol { idRol = 0, nombre = "Nuevo Rol" };
            _mockRolRepo.Setup(r => r.Agregar(It.IsAny<Rol>())).ReturnsAsync(nuevoRol);
            _mockRolRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.CrearRolAsync(nuevoRol);

            resultado.Should().NotBeNull();
            _mockRolRepo.Verify(r => r.Agregar(It.IsAny<Rol>()), Times.Once);
        }

        [Fact]
        public async Task ActualizarRolAsync_ConRolValido_DebeActualizar()
        {
            var rol = new Rol { idRol = 1, nombre = "Rol Actualizado" };
            _mockRolRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(rol);
            _mockRolRepo.Setup(r => r.Actualizar(It.IsAny<Rol>())).ReturnsAsync(rol);
            _mockRolRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarRolAsync(rol);

            resultado.Should().BeTrue();
            _mockRolRepo.Verify(r => r.Actualizar(It.IsAny<Rol>()), Times.Once);
        }

        [Fact]
        public async Task ObtenerRolPorId_ConIdValido_DebeRetornarRol()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };

            _mockRolRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(rol);

            var resultado = await _service.ObtenerRolPorId(1);

            resultado.Should().NotBeNull();
            resultado!.nombre.Should().Be("Admin");
        }

        [Fact]
        public async Task ObtenerRolPorNombre_ConNombreValido_DebeRetornarRol()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };

            _mockRolRepo.Setup(r => r.BuscarPorNombre("Admin")).ReturnsAsync(rol);

            var resultado = await _service.ObtenerRolPorNombre("Admin");

            resultado.Should().NotBeNull();
            resultado!.nombre.Should().Be("Admin");
        }

        [Fact]
        public async Task ExisteRolPorNombreAsync_ConRolExistente_DebeRetornarTrue()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };

            _mockRolRepo.Setup(r => r.BuscarPorNombre("Admin")).ReturnsAsync(rol);

            var resultado = await _service.ExisteRolPorNombreAsync("Admin");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ValidarPermisoRolAsync_ConPermisoValido_DebeRetornarTrue()
        {
            var rol = new Rol { idRol = 1, nombre = "SUPERADMIN" };

            _mockRolRepo.Setup(r => r.BuscarPorNombre("SUPERADMIN")).ReturnsAsync(rol);

            var resultado = await _service.ValidarPermisoRolAsync("SUPERADMIN", "crear_usuario");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task BuscarRolesPorNombreAsync_DebeRetornarCoincidencias()
        {
            var roles = new List<Rol>
            {
                new Rol { idRol = 1, nombre = "ADMIN" },
                new Rol { idRol = 2, nombre = "SUPERADMIN" },
                new Rol { idRol = 3, nombre = "EDITOR" }
            };
            _mockRolRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(roles);

            var resultado = await _service.BuscarRolesPorNombreAsync("ADMIN");

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ValidarRolesExistentes_DebeCrearRolesFaltantes()
        {
            _mockRolRepo.Setup(r => r.BuscarPorNombre(It.IsAny<string>())).ReturnsAsync((Rol?)null);
            _mockRolRepo.Setup(r => r.Agregar(It.IsAny<Rol>())).ReturnsAsync((Rol r) => r);
            _mockRolRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.validarRolesExistentes();

            resultado.Should().BeTrue();
            _mockRolRepo.Verify(r => r.Agregar(It.IsAny<Rol>()), Times.Exactly(3));
        }
    }
}
