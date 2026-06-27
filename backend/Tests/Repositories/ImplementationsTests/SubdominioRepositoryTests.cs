using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Repositories.Implementations;
using backend.Models;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Repositories.ImplementationsTests
{
    public class SubdominioRepositoryTests : IDisposable
    {
        private readonly NormasDb _context;
        private readonly SubdominioRepository _repository;

        public SubdominioRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NormasDb(options);
            _repository = new SubdominioRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task ObtenerPorProcesoId_DebeRetornarSubdominios_DelProcesoEspecificado()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);

            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Ind1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "Ind2", ProcesoId = 1 }
            };
            _context.Subdominios.AddRange(subdominios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorProcesoId(1);

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(s => s.ProcesoId.Should().Be(1));
        }

        [Fact]
        public async Task ObtenerPorId_DebeIncluirProcesoYActividades()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Ind1", ProcesoId = 1 };

            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorId(1);

            resultado.Should().NotBeNull();
            resultado!.Proceso.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdWithActividadesAsync_DebeRetornarActividades_DelSubdominioEspecificado()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test User", cedula = "123456", correo_electronico = "test@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            var subdominio1 = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Ind1", ProcesoId = 1 };
            var subdominio2 = new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "Ind2", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.AddRange(subdominio1, subdominio2);

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "Actividad 2", SubdominioId = 2, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 3, Nombre = "Actividad 3", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByIdWithActividadesAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Actividades.Should().HaveCount(2);
            resultado.Actividades.Should().AllSatisfy(a => a.SubdominioId.Should().Be(1));
        }

        [Fact]
        public async Task FindByPracticasGobiernoContainingAsync_DebeRetornarSubdominiosPaginados_QueCoincidenConPracticas()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);

            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Gestión de accesos", IndicadoresAsociados = "Ind1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Control de cambios", IndicadoresAsociados = "Ind2", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 3, PracticasGobierno = "Gestión de incidentes", IndicadoresAsociados = "Ind3", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 4, PracticasGobierno = "Gestión de riesgos", IndicadoresAsociados = "Ind4", ProcesoId = 1 }
            };
            _context.Subdominios.AddRange(subdominios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByPracticasGobiernoContainingAsync("Gestión", 1, 2);

            resultado.Items.Should().HaveCount(2);
            resultado.TotalCount.Should().Be(3);
            resultado.Items.Should().AllSatisfy(s => s.PracticasGobierno.Should().Contain("Gestión"));
            resultado.Items.Should().AllSatisfy(s => s.Proceso.Should().NotBeNull());
        }

        [Fact]
        public async Task FindByIndicadoresAsociadosContainingAsync_DebeRetornarSubdominiosPaginados_QueCoincidenConIndicadores()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);

            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "KPI-001 Disponibilidad", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "KPI-002 Rendimiento", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 3, PracticasGobierno = "Practica 3", IndicadoresAsociados = "KPI-003 Disponibilidad", ProcesoId = 1 }
            };
            _context.Subdominios.AddRange(subdominios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByIndicadoresAsociadosContainingAsync("Disponibilidad", 1, 10);

            resultado.Items.Should().HaveCount(2);
            resultado.TotalCount.Should().Be(2);
            resultado.Items.Should().AllSatisfy(s => s.IndicadoresAsociados.Should().Contain("Disponibilidad"));
            resultado.Items.Should().AllSatisfy(s => s.Proceso.Should().NotBeNull());
        }

        [Fact]
        public async Task GetByIdWithActividadesAsync_DebeRetornarSubdominio_ConActividadesYProceso()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test User", cedula = "123456", correo_electronico = "test@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Ind1", ProcesoId = 1 };

            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "Actividad 2", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByIdWithActividadesAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Actividades.Should().HaveCount(2);
            resultado.Proceso.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdWithProcesoAsync_DebeRetornarSubdominio_ConProcesoYDominio()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Ind1", ProcesoId = 1 };

            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            _context.Subdominios.Add(subdominio);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByIdWithProcesoAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Proceso.Should().NotBeNull();
            resultado.Proceso.Dominio.Should().NotBeNull();
            resultado.Proceso.Dominio.Nombre.Should().Be("Dominio Test");
        }

        [Fact]
        public async Task GetAllWithProcesoAsync_DebeRetornarTodosLosSubdominios_ConProcesoYDominio()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);

            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Ind1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "Ind2", ProcesoId = 1 }
            };
            _context.Subdominios.AddRange(subdominios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetAllWithProcesoAsync();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(s =>
            {
                s.Proceso.Should().NotBeNull();
                s.Proceso.Dominio.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GetByProcesoIdWithActividadesAsync_DebeRetornarSubdominios_ConActividadesYProceso()
        {
            var rol = new Rol { idRol = 1, nombre = "Admin" };
            var usuario = new Usuario { Id_Usuario = 1, nombre = "Test User", cedula = "123456", correo_electronico = "test@test.com", contrasena = "hash", idRol = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            _context.Roles.Add(rol);
            _context.Usuarios.Add(usuario);
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);

            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Ind1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "Ind2", ProcesoId = 1 }
            };
            _context.Subdominios.AddRange(subdominios);

            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "Actividad 2", SubdominioId = 2, FuncionariosResponsablesId = 1 }
            };
            _context.Actividades.AddRange(actividades);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByProcesoIdWithActividadesAsync(1);

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(s =>
            {
                s.ProcesoId.Should().Be(1);
                s.Proceso.Should().NotBeNull();
                s.Actividades.Should().NotBeEmpty();
            });
        }

        [Fact]
        public async Task ObtenerTodos_Override_DebeRetornarTodosLosSubdominios_ConProceso()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);

            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Ind1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "Ind2", ProcesoId = 1 }
            };
            _context.Subdominios.AddRange(subdominios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerTodos();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(s => s.Proceso.Should().NotBeNull());
        }
    }
}
