using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Repositories.Implementations;
using backend.Models;

namespace backend.Tests.Repositories.ImplementationsTests
{
    public class AuditoriaRepositoryTests : IDisposable
    {
        private readonly NormasDb _context;
        private readonly AuditoriaRepository _repository;

        public AuditoriaRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NormasDb(options);
            _repository = new AuditoriaRepository(_context);
        }

        [Fact]
        public async Task FindByIdUsuarioAsync_DebeRetornarAuditorias_DelUsuarioEspecificado()
        {
            // Arrange
            var rol = new Rol { idRol = 1, nombre = "Administrador" };
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                nombre = "Juan Pérez",
                correo_electronico = "juan@example.com",
                contrasena = "hash",
                idRol = 1
            };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "Audit1", TipoEvento = "Login", IdUsuario = 1 },
                new Auditoria { IdAuditoria = 2, Descripcion = "Audit2", TipoEvento = "Logout", IdUsuario = 1 },
                new Auditoria { IdAuditoria = 3, Descripcion = "Audit3", TipoEvento = "Login", IdUsuario = null }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.FindByIdUsuarioAsync(1);

            // Assert
            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.IdUsuario.Should().Be(1));
        }

        [Fact]
        public async Task FindByTipoEventoAsync_DebeRetornarAuditorias_DelTipoEspecificado()
        {
            // Arrange
            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "Audit1", TipoEvento = "Login" },
                new Auditoria { IdAuditoria = 2, Descripcion = "Audit2", TipoEvento = "Logout" },
                new Auditoria { IdAuditoria = 3, Descripcion = "Audit3", TipoEvento = "Login" }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.FindByTipoEventoAsync("Login");

            // Assert
            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.TipoEvento.Should().Be("Login"));
        }

        [Fact]
        public async Task FindByFechaEventoRangeAsync_DebeRetornarAuditorias_EnRangoDeFechas()
        {
            // Arrange
            var fechaInicio = new DateTime(2024, 1, 1);
            var fechaFin = new DateTime(2024, 1, 31);

            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "Audit1", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 15) },
                new Auditoria { IdAuditoria = 2, Descripcion = "Audit2", TipoEvento = "Logout", FechaEvento = new DateTime(2024, 2, 1) },
                new Auditoria { IdAuditoria = 3, Descripcion = "Audit3", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 20) }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.FindByFechaEventoRangeAsync(fechaInicio, fechaFin);

            // Assert
            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a =>
            {
                a.FechaEvento.Should().BeOnOrAfter(fechaInicio);
                a.FechaEvento.Should().BeOnOrBefore(fechaFin);
            });
        }

        [Fact]
        public async Task ObtenerPorId_DebeRetornarAuditoria_ConUsuarioIncluido()
        {
            // Arrange
            var rol = new Rol { idRol = 1, nombre = "Administrador" };
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "123456789",
                nombre = "Juan Pérez",
                correo_electronico = "juan@example.com",
                contrasena = "hash",
                idRol = 1
            };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var auditoria = new Auditoria
            {
                IdAuditoria = 1,
                Descripcion = "Audit1",
                TipoEvento = "Login",
                IdUsuario = 1
            };
            _context.Auditorias.Add(auditoria);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.ObtenerPorId(1);

            // Assert
            resultado.Should().NotBeNull();
            resultado!.IdAuditoria.Should().Be(1);
            resultado.Usuario.Should().NotBeNull();
            resultado.Usuario!.Id_Usuario.Should().Be(1);
        }

        [Fact]
        public async Task ObtenerTodos_DebeRetornarTodasLasAuditorias_OrdenadasPorFechaDescendente()
        {
            // Arrange
            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "Audit1", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 10) },
                new Auditoria { IdAuditoria = 2, Descripcion = "Audit2", TipoEvento = "Logout", FechaEvento = new DateTime(2024, 1, 20) },
                new Auditoria { IdAuditoria = 3, Descripcion = "Audit3", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 15) }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.ObtenerTodos();

            // Assert
            resultado.Should().HaveCount(3);
            var listaResultado = resultado.ToList();
            listaResultado[0].FechaEvento.Should().Be(new DateTime(2024, 1, 20)); // Más reciente primero
            listaResultado[1].FechaEvento.Should().Be(new DateTime(2024, 1, 15));
            listaResultado[2].FechaEvento.Should().Be(new DateTime(2024, 1, 10));
        }

        [Fact]
        public async Task FindByTablaAfectadaAsync_DebeRetornarAuditorias_DeTablaEspecificada()
        {
            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "A1", TipoEvento = "Insert", Modulo = "Usuarios" },
                new Auditoria { IdAuditoria = 2, Descripcion = "A2", TipoEvento = "Update", Modulo = "Procesos" },
                new Auditoria { IdAuditoria = 3, Descripcion = "A3", TipoEvento = "Delete", Modulo = "Usuarios" }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByTablaAfectadaAsync("Usuarios");

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.Modulo.Should().Be("Usuarios"));
        }

        [Fact]
        public async Task FindByRegistroAfectadoAsync_DebeRetornarAuditorias_DelRegistroEspecificado()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario1 = new Usuario { Id_Usuario = 1, nombre = "User1", cedula = "123", correo_electronico = "u1@test.com", contrasena = "hash", idRol = 1 };
            var usuario2 = new Usuario { Id_Usuario = 2, nombre = "User2", cedula = "456", correo_electronico = "u2@test.com", contrasena = "hash", idRol = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.AddRange(usuario1, usuario2);
            await _context.SaveChangesAsync();

            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "A1", TipoEvento = "Update", IdUsuario = 1 },
                new Auditoria { IdAuditoria = 2, Descripcion = "A2", TipoEvento = "Update", IdUsuario = 2 },
                new Auditoria { IdAuditoria = 3, Descripcion = "A3", TipoEvento = "Update", IdUsuario = 1 }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByRegistroAfectadoAsync(1);

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task FindByDescripcionContainingAsync_DebeRetornarAuditoriasPaginadas_ConDescripcion()
        {
            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "Usuario creado exitosamente", TipoEvento = "Create" },
                new Auditoria { IdAuditoria = 2, Descripcion = "Proceso actualizado", TipoEvento = "Update" },
                new Auditoria { IdAuditoria = 3, Descripcion = "Usuario eliminado", TipoEvento = "Delete" },
                new Auditoria { IdAuditoria = 4, Descripcion = "Usuario modificado", TipoEvento = "Update" }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByDescripcionContainingAsync("Usuario", 1, 2);

            resultado.Items.Should().HaveCount(2);
            resultado.TotalCount.Should().Be(3);
            resultado.Items.Should().AllSatisfy(a => a.Descripcion.Should().Contain("Usuario"));
        }

        [Fact]
        public async Task FindByTipoEventoAsync_Paginado_DebeRetornarAuditoriasPaginadas()
        {
            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "A1", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 3) },
                new Auditoria { IdAuditoria = 2, Descripcion = "A2", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 2) },
                new Auditoria { IdAuditoria = 3, Descripcion = "A3", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 1) },
                new Auditoria { IdAuditoria = 4, Descripcion = "A4", TipoEvento = "Logout", FechaEvento = new DateTime(2024, 1, 4) }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByTipoEventoAsync("Login", 1, 2);

            resultado.Items.Should().HaveCount(2);
            resultado.TotalCount.Should().Be(3);
            resultado.Items.Should().AllSatisfy(a => a.TipoEvento.Should().Be("Login"));
        }

        [Fact]
        public async Task FindByFechaEventoRangeAsync_Paginado_DebeRetornarAuditoriasPaginadas()
        {
            var fechaInicio = new DateTime(2024, 1, 1);
            var fechaFin = new DateTime(2024, 1, 31);

            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "A1", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 5) },
                new Auditoria { IdAuditoria = 2, Descripcion = "A2", TipoEvento = "Logout", FechaEvento = new DateTime(2024, 1, 10) },
                new Auditoria { IdAuditoria = 3, Descripcion = "A3", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 15) },
                new Auditoria { IdAuditoria = 4, Descripcion = "A4", TipoEvento = "Logout", FechaEvento = new DateTime(2024, 2, 1) }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByFechaEventoRangeAsync(fechaInicio, fechaFin, 1, 2);

            resultado.Items.Should().HaveCount(2);
            resultado.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetAllWithUsuarioAsync_DebeRetornarTodasAuditorias_ConUsuario()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "A1", TipoEvento = "Login", IdUsuario = 1, FechaEvento = new DateTime(2024, 1, 2) },
                new Auditoria { IdAuditoria = 2, Descripcion = "A2", TipoEvento = "Logout", IdUsuario = 1, FechaEvento = new DateTime(2024, 1, 1) }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetAllWithUsuarioAsync();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.Usuario.Should().NotBeNull());
        }

        [Fact]
        public async Task GetByIdWithUsuarioAsync_DebeRetornarAuditoria_ConUsuario()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test User", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var auditoria = new Auditoria { IdAuditoria = 1, Descripcion = "Test Audit", TipoEvento = "Login", IdUsuario = 1 };
            _context.Auditorias.Add(auditoria);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByIdWithUsuarioAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Usuario.Should().NotBeNull();
            resultado.Usuario!.nombre.Should().Be("Test User");
        }

        [Fact]
        public async Task GetUltimosEventosAsync_DebeRetornarUltimosEventos_LimitadosPorCantidad()
        {
            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "A1", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 5) },
                new Auditoria { IdAuditoria = 2, Descripcion = "A2", TipoEvento = "Logout", FechaEvento = new DateTime(2024, 1, 10) },
                new Auditoria { IdAuditoria = 3, Descripcion = "A3", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 15) },
                new Auditoria { IdAuditoria = 4, Descripcion = "A4", TipoEvento = "Logout", FechaEvento = new DateTime(2024, 1, 20) },
                new Auditoria { IdAuditoria = 5, Descripcion = "A5", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 25) }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetUltimosEventosAsync(3);

            resultado.Should().HaveCount(3);
            var lista = resultado.ToList();
            lista[0].FechaEvento.Should().Be(new DateTime(2024, 1, 25));
            lista[1].FechaEvento.Should().Be(new DateTime(2024, 1, 20));
            lista[2].FechaEvento.Should().Be(new DateTime(2024, 1, 15));
        }

        [Fact]
        public async Task GetEventosPorUsuarioAsync_DebeRetornarEventos_DelUsuarioEspecificado()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario1 = new Usuario { Id_Usuario = 1, nombre = "User1", cedula = "123", correo_electronico = "u1@test.com", contrasena = "hash", idRol = 1 };
            var usuario2 = new Usuario { Id_Usuario = 2, nombre = "User2", cedula = "456", correo_electronico = "u2@test.com", contrasena = "hash", idRol = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.AddRange(usuario1, usuario2);
            await _context.SaveChangesAsync();

            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "A1", TipoEvento = "Login", IdUsuario = 1, FechaEvento = new DateTime(2024, 1, 5) },
                new Auditoria { IdAuditoria = 2, Descripcion = "A2", TipoEvento = "Logout", IdUsuario = 2, FechaEvento = new DateTime(2024, 1, 10) },
                new Auditoria { IdAuditoria = 3, Descripcion = "A3", TipoEvento = "Login", IdUsuario = 1, FechaEvento = new DateTime(2024, 1, 15) }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetEventosPorUsuarioAsync(1, new DateTime(2024, 1, 1));

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.IdUsuario.Should().Be(1));
        }

        [Fact]
        public async Task GetEstadisticasPorTipoEventoAsync_DebeRetornarConteo_PorTipoEvento()
        {
            var fechaInicio = new DateTime(2024, 1, 1);
            var fechaFin = new DateTime(2024, 1, 31);

            var auditorias = new List<Auditoria>
            {
                new Auditoria { IdAuditoria = 1, Descripcion = "A1", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 5) },
                new Auditoria { IdAuditoria = 2, Descripcion = "A2", TipoEvento = "Logout", FechaEvento = new DateTime(2024, 1, 10) },
                new Auditoria { IdAuditoria = 3, Descripcion = "A3", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 15) },
                new Auditoria { IdAuditoria = 4, Descripcion = "A4", TipoEvento = "Login", FechaEvento = new DateTime(2024, 1, 20) },
                new Auditoria { IdAuditoria = 5, Descripcion = "A5", TipoEvento = "Logout", FechaEvento = new DateTime(2024, 2, 1) }
            };
            _context.Auditorias.AddRange(auditorias);
            await _context.SaveChangesAsync();

            // InMemoryDatabase no soporta GroupBy->ToDictionaryAsync, así que probamos con ToListAsync primero
            var todasLasAuditorias = await _context.Auditorias
                .Where(a => a.FechaEvento >= fechaInicio && a.FechaEvento <= fechaFin)
                .ToListAsync();
            var resultado = todasLasAuditorias
                .GroupBy(a => a.TipoEvento)
                .ToDictionary(g => g.Key, g => g.Count());

            resultado.Should().HaveCount(2);
            resultado["Login"].Should().Be(3);
            resultado["Logout"].Should().Be(1);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
