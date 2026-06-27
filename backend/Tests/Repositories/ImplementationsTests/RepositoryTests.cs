using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Repositories.Implementations;
using backend.Models;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Repositories.ImplementationsTests
{
    public class RepositoryTests : IDisposable
    {
        private readonly NormasDb _context;
        private readonly Repository<Dominio, int> _repository;

        public RepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NormasDb(options);
            _repository = new Repository<Dominio, int>(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Agregar_Y_ObtenerPorId_DebeFuncionar()
        {
            var dominio = new Dominio { Nombre = "Dominio Base" };

            var agregado = await _repository.Agregar(dominio);
            await _repository.GuardarCambios();

            var resultado = await _repository.ObtenerPorId(agregado.IdDominio);

            resultado.Should().NotBeNull();
            resultado!.Nombre.Should().Be("Dominio Base");
        }

        [Fact]
        public async Task Actualizar_DebePersistirCambios()
        {
            var dominio = new Dominio { Nombre = "Original" };
            await _repository.Agregar(dominio);
            await _repository.GuardarCambios();

            dominio.Nombre = "Actualizado";
            await _repository.Actualizar(dominio);
            await _repository.GuardarCambios();

            var resultado = await _repository.ObtenerPorId(dominio.IdDominio);
            resultado!.Nombre.Should().Be("Actualizado");
        }

        [Fact]
        public async Task Eliminar_DebeRetornarTrue_CuandoExiste()
        {
            var dominio = new Dominio { Nombre = "A Eliminar" };
            await _repository.Agregar(dominio);
            await _repository.GuardarCambios();

            var eliminado = await _repository.Eliminar(dominio.IdDominio);
            await _repository.GuardarCambios();

            eliminado.Should().BeTrue();
            var resultado = await _repository.ObtenerPorId(dominio.IdDominio);
            resultado.Should().BeNull();
        }

        [Fact]
        public async Task Existe_DebeRetornarResultadoCorrecto()
        {
            var dominio = new Dominio { Nombre = "Existe" };
            await _repository.Agregar(dominio);
            await _repository.GuardarCambios();

            var existe = await _repository.Existe(dominio.IdDominio);
            var noExiste = await _repository.Existe(9999);

            existe.Should().BeTrue();
            noExiste.Should().BeFalse();
        }

        [Fact]
        public async Task ObtenerTodos_DebeRetornarElementos()
        {
            await _repository.Agregar(new Dominio { Nombre = "D1" });
            await _repository.Agregar(new Dominio { Nombre = "D2" });
            await _repository.GuardarCambios();

            var resultado = await _repository.ObtenerTodos();

            resultado.Should().HaveCount(2);
        }
    }
}
