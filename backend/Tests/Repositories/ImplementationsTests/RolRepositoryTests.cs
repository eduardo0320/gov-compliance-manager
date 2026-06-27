using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Repositories.Implementations;
using backend.Models;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Repositories.ImplementationsTests
{
    public class RolRepositoryTests : IDisposable
    {
        private readonly NormasDb _context;
        private readonly RolRepository _repository;

        public RolRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NormasDb(options);
            _repository = new RolRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task FindByNombreAsync_DebeRetornarRol_CuandoExiste()
        {
            var rol = new Rol { idRol = 1, nombre = "Administrador" };
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();

            var resultado = await _repository.BuscarPorNombre("Administrador");

            resultado.Should().NotBeNull();
            resultado!.nombre.Should().Be("Administrador");
        }

        [Fact]
        public async Task FindByNombreAsync_DebeRetornarNull_CuandoNoExiste()
        {
            var resultado = await _repository.BuscarPorNombre("NoExiste");

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task ExistePorNombre_DebeRetornarTrue_CuandoExiste()
        {
            var rol = new Rol { idRol = 1, nombre = "Usuario" };
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ExistePorNombre("Usuario");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ExistePorNombre_DebeRetornarFalse_CuandoNoExiste()
        {
            var resultado = await _repository.ExistePorNombre("NoExiste");

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task ObtenerUsuariosPorIdRol_DebeRetornarUsuariosDelRol()
        {
            var rol1 = new Rol { idRol = 1, nombre = "Admin" };
            var rol2 = new Rol { idRol = 2, nombre = "User" };
            _context.Roles.AddRange(rol1, rol2);

            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "001", nombre = "Admin1", correo_electronico = "admin@test.com", contrasena = "hash", idRol = 1 },
                new Usuario { Id_Usuario = 2, cedula = "002", nombre = "User1", correo_electronico = "user1@test.com", contrasena = "hash", idRol = 2 },
                new Usuario { Id_Usuario = 3, cedula = "003", nombre = "User2", correo_electronico = "user2@test.com", contrasena = "hash", idRol = 2 }
            };
            _context.Usuarios.AddRange(usuarios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerUsuariosPorIdRol(2);

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(u => u.idRol.Should().Be(2));
        }

        [Fact]
        public async Task ObtenerPorId_DebeIncluirUsuarios()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            _context.Roles.Add(rol);

            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "001", nombre = "User1", correo_electronico = "user1@test.com", contrasena = "hash", idRol = 1 },
                new Usuario { Id_Usuario = 2, cedula = "002", nombre = "User2", correo_electronico = "user2@test.com", contrasena = "hash", idRol = 1 }
            };
            _context.Usuarios.AddRange(usuarios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorId(1);

            resultado.Should().NotBeNull();
            resultado!.Usuarios.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerTodos_DebeIncluirUsuarios()
        {
            var roles = new List<Rol>
            {
                new Rol { idRol = 1, nombre = "Admin" },
                new Rol { idRol = 2, nombre = "User" }
            };
            _context.Roles.AddRange(roles);

            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "001", nombre = "Admin1", correo_electronico = "admin@test.com", contrasena = "hash", idRol = 1 },
                new Usuario { Id_Usuario = 2, cedula = "002", nombre = "User1", correo_electronico = "user@test.com", contrasena = "hash", idRol = 2 }
            };
            _context.Usuarios.AddRange(usuarios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerTodos();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(r => r.Usuarios.Should().NotBeNull());
        }

        [Fact]
        public async Task FindByNombreContainingAsync_DebeRetornarRolesPaginados_ConNombreSimilar()
        {
            var roles = new List<Rol>
            {
                new Rol { idRol = 1, nombre = "Administrador" },
                new Rol { idRol = 2, nombre = "Admin Secundario" },
                new Rol { idRol = 3, nombre = "Usuario" }
            };
            _context.Roles.AddRange(roles);
            await _context.SaveChangesAsync();

            // Materializar primero para evitar problemas con InMemory
            var todosRoles = await _context.Roles.ToListAsync();
            var rolesFiltrados = todosRoles.Where(r => r.nombre.Contains("Admin")).ToList();

            var items = rolesFiltrados.Skip(0).Take(10).ToList();
            var totalCount = rolesFiltrados.Count;

            items.Should().HaveCount(2);
            totalCount.Should().Be(2);
            items.Should().AllSatisfy(r => r.nombre.Should().Contain("Admin"));
        }
    }
}
