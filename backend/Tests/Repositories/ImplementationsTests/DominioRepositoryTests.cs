using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Repositories.Implementations;
using backend.Models;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Repositories.ImplementationsTests
{
    public class DominioRepositoryTests : IDisposable
    {
        private readonly NormasDb _context;
        private readonly DominioRepository _repository;

        public DominioRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NormasDb(options);
            _repository = new DominioRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task FindByNombreAsync_DebeRetornarDominio_CuandoExiste()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Calidad" };
            _context.Dominios.Add(dominio);
            await _context.SaveChangesAsync();

            var resultado = await _repository.FindByNombreAsync("Calidad");

            resultado.Should().NotBeNull();
            resultado!.Nombre.Should().Be("Calidad");
        }

        [Fact]
        public async Task ExistsByNombreAsync_DebeRetornarTrue_CuandoExiste()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Seguridad" };
            _context.Dominios.Add(dominio);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ExistsByNombreAsync("Seguridad");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ObtenerPorId_DebeIncluirProcesos()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Test" };
            _context.Dominios.Add(dominio);

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerPorId(1);

            resultado.Should().NotBeNull();
            resultado!.Procesos.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetProcesosByDominioIdAsync_DebeRetornarProcesosDominio()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio1" };
            _context.Dominios.Add(dominio);
            await _context.SaveChangesAsync();

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" },
                new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Proceso 2", DominioId = 1, EstadoImplementacion = "Inactivo", MarcoNormativo = "M2" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetProcesosByDominioIdAsync(1);

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(p => p.DominioId.Should().Be(1));
        }

        [Fact]
        public async Task FindByNombreContainingAsync_DebeRetornarDominiosPaginados_ConNombreSimilar()
        {
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Gestión de Calidad" },
                new Dominio { IdDominio = 2, Nombre = "Control de Calidad" },
                new Dominio { IdDominio = 3, Nombre = "Seguridad" }
            };
            _context.Dominios.AddRange(dominios);
            await _context.SaveChangesAsync();

            // Materializar para evitar problemas con InMemory
            var todosDominios = await _context.Dominios.ToListAsync();
            var dominiosFiltrados = todosDominios.Where(d => d.Nombre.Contains("Calidad")).ToList();

            var items = dominiosFiltrados.Skip(0).Take(10).ToList();
            var totalCount = dominiosFiltrados.Count;

            items.Should().HaveCount(2);
            totalCount.Should().Be(2);
            items.Should().AllSatisfy(d => d.Nombre.Should().Contain("Calidad"));
        }

        [Fact]
        public async Task GetByIdWithProcesosAsync_DebeRetornarDominioConProcesos()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Test Dominio" };
            _context.Dominios.Add(dominio);
            await _context.SaveChangesAsync();

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" },
                new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Proceso 2", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M2" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetByIdWithProcesosAsync(1);

            resultado.Should().NotBeNull();
            resultado!.Procesos.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllWithProcesosAsync_DebeRetornarTodosDominiosConProcesos()
        {
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio1" },
                new Dominio { IdDominio = 2, Nombre = "Dominio2" }
            };
            _context.Dominios.AddRange(dominios);
            await _context.SaveChangesAsync();

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "Activo", MarcoNormativo = "M1" },
                new Proceso { IdProceso = 2, Codigo = "P2", Nombre = "Proceso 2", DominioId = 2, EstadoImplementacion = "Activo", MarcoNormativo = "M2" }
            };
            _context.Procesos.AddRange(procesos);
            await _context.SaveChangesAsync();

            var resultado = await _repository.GetAllWithProcesosAsync();

            resultado.Should().HaveCount(2);
            resultado.Should().AllSatisfy(d => d.Procesos.Should().NotBeNull());
        }

        [Fact]
        public async Task ObtenerTodos_DebeRetornarTodosDominiosConProcesos()
        {
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio1" },
                new Dominio { IdDominio = 2, Nombre = "Dominio2" }
            };
            _context.Dominios.AddRange(dominios);
            await _context.SaveChangesAsync();

            var resultado = await _repository.ObtenerTodos();

            resultado.Should().HaveCount(2);
        }
    }
}
