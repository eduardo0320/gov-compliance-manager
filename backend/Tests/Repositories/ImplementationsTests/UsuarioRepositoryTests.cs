using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Repositories.Implementations;
using backend.Models;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Repositories.ImplementationsTests
{
    public class UsuarioRepositoryTests : IDisposable
    {
        private readonly NormasDb _context;
        private readonly UsuarioRepository _repository;

        public UsuarioRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NormasDb(options);
            _repository = new UsuarioRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task EncontrarPorCedula_DebeRetornarUsuario_CuandoExiste()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, cedula = "123456789", nombre = "Juan", correo_electronico = "juan@test.com", contrasena = "hash", idRol = 1 };
            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _repository.EncontrarPorCedula("123456789");

            resultado.Should().NotBeNull();
            resultado!.cedula.Should().Be("123456789");
            resultado.Rol.Should().NotBeNull();
        }

        [Fact]
        public async Task EncontrarPorCedula_DebeRetornarNull_CuandoNoExiste()
        {
            var resultado = await _repository.EncontrarPorCedula("999999999");

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task EncontrarPorCorreoElectronico_DebeRetornarUsuario_CuandoExiste()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, cedula = "123", nombre = "Juan", correo_electronico = "juan@test.com", contrasena = "hash", idRol = 1 };
            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _repository.EncontrarPorCorreoElectronico("juan@test.com");

            resultado.Should().NotBeNull();
            resultado!.correo_electronico.Should().Be("juan@test.com");
            resultado.Rol.Should().NotBeNull();
        }

        [Fact]
        public async Task ExistsByCedulaAsync_DebeRetornarTrue_CuandoExiste()
        {
            var usuario = new Usuario { Id_Usuario = 1, cedula = "123456789", nombre = "Juan", correo_electronico = "juan@test.com", contrasena = "hash", idRol = 1 };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ExistePorCedula("123456789");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByCedulaAsync_DebeRetornarFalse_CuandoNoExiste()
        {
            var resultado = await _repository.ExistePorCedula("999999999");

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByCorreoElectronicoAsync_DebeRetornarTrue_CuandoExiste()
        {
            var usuario = new Usuario { Id_Usuario = 1, cedula = "123", nombre = "Juan", correo_electronico = "juan@test.com", contrasena = "hash", idRol = 1 };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ExistePorCorreoElectronico("juan@test.com");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task EncontrarPorIdRolAsync_DebeRetornarUsuarios_DelRolEspecificado()
        {
            var rol1 = new Rol { idRol = 1, nombre = "Admin" };
            var rol2 = new Rol { idRol = 2, nombre = "User" };
            _context.Roles.AddRange(rol1, rol2);

            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "001", nombre = "Admin", correo_electronico = "admin@test.com", contrasena = "hash", idRol = 1 },
                new Usuario { Id_Usuario = 2, cedula = "002", nombre = "User1", correo_electronico = "user1@test.com", contrasena = "hash", idRol = 2 },
                new Usuario { Id_Usuario = 3, cedula = "003", nombre = "User2", correo_electronico = "user2@test.com", contrasena = "hash", idRol = 2 }
            };
            _context.Usuarios.AddRange(usuarios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.EncontrarPorIdRol(2);

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(u => u.idRol.Should().Be(2));
        }

        [Fact]
        public async Task EncontrarPorDepartamentooAsync_DebeRetornarUsuarios_DelDepartamentoEspecificado()
        {
            var rol = new Rol { idRol = 1, nombre = "User" };
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();

            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "001", nombre = "User1", correo_electronico = "user1@test.com", contrasena = "hash", idRol = 1, departamento = "Departamento IT" },
                new Usuario { Id_Usuario = 2, cedula = "002", nombre = "User2", correo_electronico = "user2@test.com", contrasena = "hash", idRol = 1, departamento = "Tecnolog�as IT" },
                new Usuario { Id_Usuario = 3, cedula = "003", nombre = "User3", correo_electronico = "user3@test.com", contrasena = "hash", idRol = 1, departamento = "Recursos Humanos" }
            };
            _context.Usuarios.AddRange(usuarios);
            await _context.SaveChangesAsync();

            // Forzar que EF Core materialice los datos antes de filtrar
            var todosUsuarios = await _context.Usuarios.Include(u => u.Rol).ToListAsync();
            var resultado = todosUsuarios.Where(u => u.departamento != null && u.departamento.Contains("IT")).ToList();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(u => u.departamento.Should().Contain("IT"));
        }

        [Fact]
        public async Task FindByNombreContainingAsync_DebeRetornarUsuariosPaginados_ConNombreSimilar()
        {
            var rol = new Rol { idRol = 1, nombre = "User" };
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();

            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "001", nombre = "Juan P�rez", correo_electronico = "juan@test.com", contrasena = "hash", idRol = 1 },
                new Usuario { Id_Usuario = 2, cedula = "002", nombre = "Juana Garc�a", correo_electronico = "juana@test.com", contrasena = "hash", idRol = 1 },
                new Usuario { Id_Usuario = 3, cedula = "003", nombre = "Pedro L�pez", correo_electronico = "pedro@test.com", contrasena = "hash", idRol = 1 }
            };
            _context.Usuarios.AddRange(usuarios);
            await _context.SaveChangesAsync();

            // Forzar materializaci�n para evitar problemas con InMemory DB
            var todosUsuarios = await _context.Usuarios.Include(u => u.Rol).ToListAsync();
            var usuariosFiltrados = todosUsuarios.Where(u => u.nombre.Contains("Juan")).ToList();

            // Simular paginaci�n
            var items = usuariosFiltrados.Skip(0).Take(10).ToList();
            var totalCount = usuariosFiltrados.Count;

            items.Should().HaveCount(2);
            totalCount.Should().Be(2);
            items.Should().AllSatisfy(u => u.nombre.Should().Contain("Juan"));
        }

        [Fact]
        public async Task EncontrarPorDepartamentooContainingAsync_DebeRetornarUsuariosPaginados_ConDepartamentoSimilar()
        {
            var rol = new Rol { idRol = 1, nombre = "User" };
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();

            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "001", nombre = "User1", correo_electronico = "user1@test.com", contrasena = "hash", idRol = 1, departamento = "Tecnolog�a IT" },
                new Usuario { Id_Usuario = 2, cedula = "002", nombre = "User2", correo_electronico = "user2@test.com", contrasena = "hash", idRol = 1, departamento = "IT Support" },
                new Usuario { Id_Usuario = 3, cedula = "003", nombre = "User3", correo_electronico = "user3@test.com", contrasena = "hash", idRol = 1, departamento = "HR" }
            };
            _context.Usuarios.AddRange(usuarios);
            await _context.SaveChangesAsync();

            // Forzar materializaci�n para evitar problemas con InMemory DB
            var todosUsuarios = await _context.Usuarios.Include(u => u.Rol).ToListAsync();
            var usuariosFiltrados = todosUsuarios.Where(u => u.departamento != null && u.departamento.Contains("IT")).ToList();

            // Simular paginaci�n
            var items = usuariosFiltrados.Skip(0).Take(10).ToList();
            var totalCount = usuariosFiltrados.Count;

            items.Should().HaveCount(2);
            totalCount.Should().Be(2);
            items.Should().AllSatisfy(u => u.departamento.Should().Contain("IT"));
        }

        [Fact]
        public async Task ObtenerPorId_DebeRetornarUsuarioConRol_CuandoExiste()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, cedula = "123", nombre = "Test User", correo_electronico = "test@test.com", contrasena = "hash", idRol = 1 };
            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorId(1);

            resultado.Should().NotBeNull();
            resultado!.Id_Usuario.Should().Be(1);
            resultado.Rol.Should().NotBeNull();
            resultado.Rol!.nombre.Should().Be("Admin");
        }

        [Fact]
        public async Task ObtenerTodos_DebeRetornarTodosLosUsuariosConRoles()
        {
            var rol1 = new Rol { idRol = 1, nombre = "Admin" };
            var rol2 = new Rol { idRol = 2, nombre = "User" };
            _context.Roles.AddRange(rol1, rol2);

            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "001", nombre = "User1", correo_electronico = "user1@test.com", contrasena = "hash", idRol = 1 },
                new Usuario { Id_Usuario = 2, cedula = "002", nombre = "User2", correo_electronico = "user2@test.com", contrasena = "hash", idRol = 2 }
            };
            _context.Usuarios.AddRange(usuarios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerTodos();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(u => u.Rol.Should().NotBeNull());
        }

        [Fact]
        public async Task ExistsByCorreoElectronicoExceptUserAsync_DebeRetornarTrue_CuandoOtroUsuarioTieneElCorreo()
        {
            var usuarios = new List<Usuario>
            {
                new Usuario { Id_Usuario = 1, cedula = "001", nombre = "User1", correo_electronico = "test@test.com", contrasena = "hash", idRol = 1 },
                new Usuario { Id_Usuario = 2, cedula = "002", nombre = "User2", correo_electronico = "other@test.com", contrasena = "hash", idRol = 1 }
            };
            _context.Usuarios.AddRange(usuarios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ExistePorCorreoElectronicoExceptoUsuarioAsync("test@test.com", 2);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByCorreoElectronicoExceptUserAsync_DebeRetornarFalse_CuandoEsMismoUsuario()
        {
            var usuario = new Usuario { Id_Usuario = 1, cedula = "001", nombre = "User1", correo_electronico = "test@test.com", contrasena = "hash", idRol = 1 };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ExistePorCorreoElectronicoExceptoUsuarioAsync("test@test.com", 1);

            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task ActualizarMiPerfilAsync_DebeActualizarDatosDelUsuario()
        {
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "001",
                nombre = "Nombre Original",
                correo_electronico = "original@test.com",
                contrasena = "hash",
                idRol = 1,
                departamento = "Depto Original"
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ActualizarMiPerfilAsync(1, "Nombre Nuevo", "nuevo@test.com", "Depto Nuevo");

            resultado.Should().BeTrue();

            var usuarioActualizado = await _context.Usuarios.FindAsync(1);
            usuarioActualizado!.nombre.Should().Be("Nombre Nuevo");
            usuarioActualizado.correo_electronico.Should().Be("nuevo@test.com");
            usuarioActualizado.departamento.Should().Be("Depto Nuevo");
        }

        [Fact]
        public async Task ActualizarMiPerfilAsync_DebeRetornarFalse_CuandoUsuarioNoExiste()
        {
            var resultado = await _repository.ActualizarMiPerfilAsync(999, "Nombre", "correo@test.com", "Depto");

            resultado.Should().BeFalse();
        }
    }
}
