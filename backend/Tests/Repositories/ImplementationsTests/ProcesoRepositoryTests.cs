using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Repositories.Implementations;
using backend.Models;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Repositories.ImplementationsTests
{
    public class ProcesoRepositoryTests : IDisposable
    {
        private readonly NormasDb _context;
        private readonly ProcesoRepository _repository;

        public ProcesoRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NormasDb(options);
            _repository = new ProcesoRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task ObtenerPorCodigo_DebeRetornarProceso_CuandoExiste()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "PROC001", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "Marco 1" };
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorCodigo("PROC001");

            resultado.Should().NotBeNull();
            resultado!.Codigo.Should().Be("PROC001");
            resultado.Dominio.Should().NotBeNull();
        }

        [Fact]
        public async Task ExistePorCodigo_DebeRetornarTrue_CuandoExiste()
        {
            var proceso = new Proceso { IdProceso = 1, Codigo = "PROC001", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "Marco 1" };
            _context.Procesos.Add(proceso);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ExistePorCodigo("PROC001");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task EncontrarPorIdDominio_DebeRetornarProcesos_DelDominioEspecificado()
        {
            var dominio1 = new Dominio { IdDominio = 1, Nombre = "Dominio 1" };
            var dominio2 = new Dominio { IdDominio = 2, Nombre = "Dominio 2" };
            _context.Dominios.AddRange(dominio1, dominio2);

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" },
                new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Proceso 2", DominioId = 2, EstadoImplementacion = "Activo", MarcoNormativo = "M2" },
                new Proceso { IdProceso = 3, Codigo = "P3", Nombre = "Proceso 3", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M3" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            var resultado = await _repository.EncontrarPorIdDominio(1);

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(p => p.DominioId.Should().Be(1));
        }

        [Fact]
        public async Task EncontrarPorEstadoImplementacion_DebeRetornarProcesos_ConEstadoEspecificado()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            _context.Dominios.Add(dominio);

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" },
                new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Proceso 2", DominioId = 1, EstadoImplementacion = "Inactivo", MarcoNormativo = "M2" },
                new Proceso { IdProceso = 3, Codigo = "P3", Nombre = "Proceso 3", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M3" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            var resultado = await _repository.EncontrarPorEstadoImplementacion("Activo");

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(p => p.EstadoImplementacion.Should().Be("Activo"));
        }

        [Fact]
        public async Task ObtenerPorId_DebeIncluirDominioYSubdominios()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "PROC001", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);

            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Indicador 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "Indicador 2", ProcesoId = 1 }
            };
            _context.Subdominios.AddRange(subdominios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorId(1);

            resultado.Should().NotBeNull();
            resultado!.Dominio.Should().NotBeNull();
            resultado.Subdominios.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerSubdominiosPorIdProceso_DebeRetornarSubdominios_DelProcesoEspecificado()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso1 = new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            var proceso2 = new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Proceso 2", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M2" };
            _context.Dominios.Add(dominio);
            _context.Procesos.AddRange(proceso1, proceso2);

            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Indicador 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "Indicador 2", ProcesoId = 2 },
                new Subdominio { IdSubdominio = 3, PracticasGobierno = "Practica 3", IndicadoresAsociados = "Indicador 3", ProcesoId = 1 }
            };
            _context.Subdominios.AddRange(subdominios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerSubdominiosPorIdProceso(1);

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(s => s.ProcesoId.Should().Be(1));
            resultado.Should().AllSatisfy(s => s.Proceso.Should().NotBeNull());
        }

        [Fact]
        public async Task FindByNombreContainingAsync_DebeRetornarProcesosPaginados_QueCoincidenConNombre()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            _context.Dominios.Add(dominio);

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Gestión de Usuarios", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" },
                new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Control de Acceso", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M2" },
                new Proceso { IdProceso = 3, Codigo = "P3", Nombre = "Gestión de Proyectos", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M3" },
                new Proceso { IdProceso = 4, Codigo = "P4", Nombre = "Gestión de Riesgos", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M4" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            // Materializamos los datos primero
            var todos = await _context.Procesos.Include(p => p.Dominio).ToListAsync();
            var filtrados = todos.Where(p => p.Nombre.Contains("Gestión")).ToList();
            var items = filtrados.Skip(0).Take(2).ToList();

            var resultado = await _repository.FindByNombreContainingAsync("Gestión", 1, 2);

            resultado.Items.Should().HaveCount(2);
            resultado.TotalCount.Should().Be(3);
            resultado.Items.Should().AllSatisfy(p => p.Nombre.Should().Contain("Gestión"));
            resultado.Items.Should().AllSatisfy(p => p.Dominio.Should().NotBeNull());
        }

        [Fact]
        public async Task FindByMarcoNormativoContainingAsync_DebeRetornarProcesosPaginados_QueCoincidenConMarco()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            _context.Dominios.Add(dominio);

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "ISO 27001" },
                new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Proceso 2", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "ISO 9001" },
                new Proceso { IdProceso = 3, Codigo = "P3", Nombre = "Proceso 3", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "ISO 27002" },
                new Proceso { IdProceso = 4, Codigo = "P4", Nombre = "Proceso 4", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "NIST" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            // Materializamos los datos primero - página 2 con tamaño 2 debería tener solo 1 item (de 3 totales con "ISO")
            var todos = await _context.Procesos.Include(p => p.Dominio).ToListAsync();
            var filtrados = todos.Where(p => p.MarcoNormativo.Contains("ISO")).ToList();
            var items = filtrados.Skip(1).Take(2).ToList();

            var resultado = await _repository.FindByMarcoNormativoContainingAsync("ISO", 2, 2);

            resultado.Items.Should().HaveCount(1); // Página 2: items del 3 al 3 (solo 1)
            resultado.TotalCount.Should().Be(3);
            resultado.Items.Should().AllSatisfy(p => p.MarcoNormativo.Should().Contain("ISO"));
            resultado.Items.Should().AllSatisfy(p => p.Dominio.Should().NotBeNull());
        }

        [Fact]
        public async Task GetByIdWithSubdominiosAsync_DebeRetornarProceso_ConSubdominiosYDominio()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "PROC001", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);

            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Indicador 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "Indicador 2", ProcesoId = 1 }
            };
            _context.Subdominios.AddRange(subdominios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByIdWithSubdominiosAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Subdominios.Should().HaveCount(2);
            resultado.Dominio.Should().NotBeNull();
            resultado.Dominio.Nombre.Should().Be("Dominio Test");
        }

        [Fact]
        public async Task GetByIdWithDominioAsync_DebeRetornarProceso_ConDominio()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            var proceso = new Proceso { IdProceso = 1, Codigo = "PROC001", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" };
            _context.Dominios.Add(dominio);
            _context.Procesos.Add(proceso);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByIdWithDominioAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Dominio.Should().NotBeNull();
            resultado.Dominio.Nombre.Should().Be("Dominio Test");
        }

        [Fact]
        public async Task GetAllWithDominioAsync_DebeRetornarTodosLosProcesos_ConDominio()
        {
            var dominio1 = new Dominio { IdDominio = 1, Nombre = "Dominio 1" };
            var dominio2 = new Dominio { IdDominio = 2, Nombre = "Dominio 2" };
            _context.Dominios.AddRange(dominio1, dominio2);

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" },
                new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Proceso 2", DominioId = 2, EstadoImplementacion = "Activo", MarcoNormativo = "M2" },
                new Proceso { IdProceso = 3, Codigo = "P3", Nombre = "Proceso 3", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M3" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetAllWithDominioAsync();

            resultado.Should().HaveCount(3);
            resultado.Should().AllSatisfy(p => p.Dominio.Should().NotBeNull());
        }

        [Fact]
        public async Task ObtenerTodos_Override_DebeRetornarTodosLosProcesos_ConDominio()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            _context.Dominios.Add(dominio);

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" },
                new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Proceso 2", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M2" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerTodos();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(p => p.Dominio.Should().NotBeNull());
        }
    }
}
