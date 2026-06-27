using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Repositories.Implementations;
using backend.Models;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Repositories.ImplementationsTests
{
    public class ActividadRepositoryTests : IDisposable
    {
        private readonly NormasDb _context;
        private readonly ActividadRepository _repository;

        public ActividadRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NormasDb(options);
            _repository = new ActividadRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task ObtenerPorIdSubdominio_DebeRetornarActividades_DelSubdominioEspecificado()
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
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            await _context.SaveChangesAsync();

            var proceso = new Proceso
            {
                IdProceso = 1,
                Codigo = "P1",
                Nombre = "Proceso1",
                MarcoNormativo = "Marco1",
                EstadoImplementacion = "Sí",
                DominioId = 1,
                CreadoPorId = 1,
                ModificadoPorId = 1
            };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Práctica1", IndicadoresAsociados = "Indicadores1", ProcesoId = 1 };

            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad
                {
                    IdActividad = 1,
                    Nombre = "Act1",
                    EstadoImplementacion = "Pendiente",
                    Implementable = "Sí",
                    SubdominioId = 1,
                    FuncionariosResponsablesId = 1
                }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _repository.ObtenerPorIdSubdominio(1);

            // Assert
            resultado.Should().HaveCount(1);
            resultado.First().SubdominioId.Should().Be(1);
        }

        [Fact]
        public async Task EncontrarPorEstadoImplementacion_DebeRetornarActividades_ConEstadoEspecificado()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", EstadoImplementacion = "Pendiente", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", EstadoImplementacion = "Implementado", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "A3", EstadoImplementacion = "Pendiente", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorEstadoImplementacion("Pendiente");

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.EstadoImplementacion.Should().Be("Pendiente"));
        }

        [Fact]
        public async Task ObtenerPorImplementable_DebeRetornarActividades_ImplementablesONo()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", Implementable = "Sí", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", Implementable = "No", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "A3", Implementable = "Sí", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorImplementable("Sí");

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.Implementable.Should().Be("Sí"));
        }

        [Fact]
        public async Task ObtenerPorIdFuncionariosResponsables_DebeRetornarActividades_DelFuncionario()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario1 = new Usuario { Id_Usuario = 1, nombre = "Usuario1", cedula = "123", correo_electronico = "u1@test.com", contrasena = "hash", idRol = 1 };
            var usuario2 = new Usuario { Id_Usuario = 2, nombre = "Usuario2", cedula = "456", correo_electronico = "u2@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.AddRange(usuario1, usuario2);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", SubdominioId = 1, FuncionariosResponsablesId = 2 },
                new Actividad { IdActividad = 3, Nombre = "A3", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorIdFuncionariosResponsables(1);

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.FuncionariosResponsablesId.Should().Be(1));
        }

        [Fact]
        public async Task ObtenerPorRangoDeFechaCompromiso_DebeRetornarActividades_EnRangoFechas()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", FechaCompromiso = new DateTime(2024, 1, 15), SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", FechaCompromiso = new DateTime(2024, 3, 15), SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "A3", FechaCompromiso = new DateTime(2024, 2, 15), SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorRangoDeFechaCompromiso(new DateTime(2024, 2, 1), new DateTime(2024, 2, 28));

            resultado.Should().HaveCount(1);
            resultado.First().FechaCompromiso.Should().Be(new DateTime(2024, 2, 15));
        }

        [Fact]
        public async Task ObtenerPorRangoDePorcentajeAvance_DebeRetornarActividades_EnRangoPorcentaje()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", PorcentajeAvance = 25.50m, SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", PorcentajeAvance = 75.00m, SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "A3", PorcentajeAvance = 50.00m, SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorRangoDePorcentajeAvance(40.00m, 80.00m);

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.PorcentajeAvance.Should().BeInRange(40.00m, 80.00m));
        }

        [Fact]
        public async Task FindByNombreContainingAsync_DebeRetornarActividadesPaginadas_QueCoincidenConNombre()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Revisar documentación", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "Actualizar sistema", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "Revisar código", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByNombreContainingAsync("Revisar", 1, 10);

            resultado.Items.Should().HaveCount(2);
            resultado.TotalCount.Should().Be(2);
            resultado.Items.Should().AllSatisfy(a => a.Nombre.Should().Contain("Revisar"));
        }

        [Fact]
        public async Task EncontrarPorEstadoImplementacion_Paginado_DebeRetornarActividadesPaginadas()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", EstadoImplementacion = "Pendiente", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", EstadoImplementacion = "Pendiente", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "A3", EstadoImplementacion = "Pendiente", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.EncontrarPorEstadoImplementacion("Pendiente", 1, 2);

            resultado.Items.Should().HaveCount(2);
            resultado.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task FindByObservacionesContainingAsync_DebeRetornarActividadesPaginadas_ConObservaciones()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", Observaciones = "Requiere aprobación urgente", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", Observaciones = "En proceso de revisión", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "A3", Observaciones = "Requiere recursos adicionales", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByObservacionesContainingAsync("Requiere", 1, 10);

            resultado.Items.Should().HaveCount(2);
            resultado.TotalCount.Should().Be(2);
            resultado.Items.Should().AllSatisfy(a => a.Observaciones.Should().Contain("Requiere"));
        }

        [Fact]
        public async Task GetByIdWithFuncionarioResponsableAsync_DebeRetornarActividad_ConFuncionario()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test User", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividad = new Actividad { IdActividad = 1, Nombre = "A1", SubdominioId = 1, FuncionariosResponsablesId = 1 };
            _context.Actividades.Add(actividad);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByIdWithFuncionarioResponsableAsync(1);

            resultado.Should().NotBeNull();
            resultado!.FuncionariosResponsables.Should().NotBeNull();
            resultado.FuncionariosResponsables!.nombre.Should().Be("Test User");
        }

        [Fact]
        public async Task GetByIdWithSubdominioAsync_DebeRetornarActividad_ConSubdominioProcesoYDominio()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test User", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso Test", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica Test", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividad = new Actividad { IdActividad = 1, Nombre = "Actividad Test", SubdominioId = 1, FuncionariosResponsablesId = 1 };
            _context.Actividades.Add(actividad);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByIdWithSubdominioAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Subdominio.Should().NotBeNull();
            resultado.Subdominio.Proceso.Should().NotBeNull();
            resultado.Subdominio.Proceso.Dominio.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllWithFuncionarioResponsableAsync_DebeRetornarTodasActividades_ConFuncionarios()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetAllWithFuncionarioResponsableAsync();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.FuncionariosResponsables.Should().NotBeNull());
        }

        [Fact]
        public async Task ObtenerActividadesPendientesAsync_DebeRetornarSoloActividades_ConEstadoPendiente()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", EstadoImplementacion = "Pendiente", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", EstadoImplementacion = "Implementado", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "A3", EstadoImplementacion = "Pendiente", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerActividadesPendientesAsync();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a => a.EstadoImplementacion.Should().Be("Pendiente"));
        }

        [Fact]
        public async Task ObtenerActividadesVencidasAsync_DebeRetornarActividades_VencidasYNoImplementadas()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", FechaCompromiso = DateTime.Now.AddDays(-10), EstadoImplementacion = "Pendiente", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", FechaCompromiso = DateTime.Now.AddDays(-5), EstadoImplementacion = "Implementado", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "A3", FechaCompromiso = DateTime.Now.AddDays(5), EstadoImplementacion = "Pendiente", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerActividadesVencidasAsync();

            resultado.Should().HaveCount(1);
            resultado.First().IdActividad.Should().Be(1);
        }

        [Fact]
        public async Task ObtenerPromedioAvancePorSubdominioAsync_DebeCalcularPromedio_DeActividadesDelSubdominio()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", PorcentajeAvance = 30.00m, SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", PorcentajeAvance = 50.00m, SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "A3", PorcentajeAvance = 70.00m, SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var promedio = await _repository.ObtenerPromedioAvancePorSubdominioAsync(1);

            promedio.Should().Be(50.00m);
        }

        [Fact]
        public async Task ObtenerPorId_Override_DebeRetornarActividad_ConTodasLasRelaciones()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividad = new Actividad { IdActividad = 1, Nombre = "A1", SubdominioId = 1, FuncionariosResponsablesId = 1 };
            _context.Actividades.Add(actividad);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorId(1);

            resultado.Should().NotBeNull();
            resultado!.FuncionariosResponsables.Should().NotBeNull();
            resultado.Subdominio.Should().NotBeNull();
            resultado.Subdominio.Proceso.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerTodos_Override_DebeRetornarTodasActividades_ConRelaciones()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test", cedula = "123", correo_electronico = "t@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "D1" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso1", MarcoNormativo = "M1", EstadoImplementacion = "Activo", DominioId = 1, CreadoPorId = 1, ModificadoPorId = 1 };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "P1", IndicadoresAsociados = "I1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "A1", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "A2", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerTodos();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(a =>
            {
                a.FuncionariosResponsables.Should().NotBeNull();
                a.Subdominio.Should().NotBeNull();
            });
        }
    }
}