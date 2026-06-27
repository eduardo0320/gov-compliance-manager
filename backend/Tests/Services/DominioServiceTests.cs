using Xunit;
using Moq;
using FluentAssertions;
using backend.Models;
using backend.Services.Implementations;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Tests.Services
{
    public class DominioServiceTests
    {
        private readonly Mock<IDominioRepository> _mockDominioRepo;
        private readonly Mock<IProcesoRepository> _mockProcesoRepo;
        private readonly Mock<ISubdominioRepository> _mockSubdominioRepo;
        private readonly Mock<ILogger<DominioService>> _mockLogger;
        private readonly DominioService _service;

        public DominioServiceTests()
        {
            _mockDominioRepo = new Mock<IDominioRepository>();
            _mockProcesoRepo = new Mock<IProcesoRepository>();
            _mockSubdominioRepo = new Mock<ISubdominioRepository>();
            _mockLogger = new Mock<ILogger<DominioService>>();

            _service = new DominioService(
                _mockDominioRepo.Object,
                _mockProcesoRepo.Object,
                _mockSubdominioRepo.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ObtenerTodosLosDominiosAsync_DebeRetornarTodosLosDominios()
        {
            // Arrange
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" },
                new Dominio { IdDominio = 2, Nombre = "Dominio 2" }
            };

            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            // Act
            var resultado = await _service.ObtenerTodosLosDominiosAsync();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Should().HaveCount(2);
            _mockDominioRepo.Verify(r => r.ObtenerTodos(), Times.Once);
        }

        [Fact]
        public async Task CrearDominioAsync_ConNombreValido_DebeCrearDominio()
        {
            // Arrange
            _mockDominioRepo.Setup(r => r.FindByNombreAsync(It.IsAny<string>())).ReturnsAsync((Dominio?)null);
            _mockDominioRepo.Setup(r => r.Agregar(It.IsAny<Dominio>())).ReturnsAsync(new Dominio { IdDominio = 1 });
            _mockDominioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.CrearDominioAsync("Nuevo Dominio");

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockDominioRepo.Verify(r => r.Agregar(It.IsAny<Dominio>()), Times.Once);
        }

        [Fact]
        public async Task CrearDominioAsync_ConNombreExistente_DebeRetornarError()
        {
            // Arrange
            var dominioExistente = new Dominio { IdDominio = 1, Nombre = "Dominio Existente" };
            var dominios = new List<Dominio> { dominioExistente };
            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            // Act
            var resultado = await _service.CrearDominioAsync("Dominio Existente");

            // Assert
            resultado.Should().Contain("Error");
            resultado.Should().ContainAny("ya existe", "Ya existe");
            _mockDominioRepo.Verify(r => r.Agregar(It.IsAny<Dominio>()), Times.Never);
        }

        [Fact]
        public async Task ActualizarDominioAsync_ConDatosValidos_DebeActualizar()
        {
            // Arrange
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Original" };
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);
            _mockDominioRepo.Setup(r => r.Actualizar(It.IsAny<Dominio>())).ReturnsAsync(dominio);
            _mockDominioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.ActualizarDominioAsync(1, "Dominio Actualizado");

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockDominioRepo.Verify(r => r.Actualizar(It.Is<Dominio>(d => d.Nombre == "Dominio Actualizado")), Times.Once);
        }

        [Fact]
        public async Task EliminarDominioAsync_ConIdValido_DebeEliminarDominio()
        {
            // Arrange
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio a Eliminar" };
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);
            _mockDominioRepo.Setup(r => r.Eliminar(1)).ReturnsAsync(true);
            _mockDominioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.EliminarDominioAsync(1);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockDominioRepo.Verify(r => r.Eliminar(1), Times.Once);
        }

        [Fact]
        public async Task ObtenerDominioPorIdAsync_ConIdValido_DebeRetornarDominio()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };

            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);

            var resultado = await _service.ObtenerDominioPorIdAsync(1);

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ExisteDominioPorNombreAsync_ConDominioExistente_DebeRetornarTrue()
        {
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio Test" }
            };

            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            var resultado = await _service.ExisteDominioPorNombreAsync("Dominio Test");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ObtenerDominioPorNombreAsync_ConNombreValido_DebeRetornarDominio()
        {
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio Test" }
            };

            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            var resultado = await _service.ObtenerDominioPorNombreAsync("Dominio Test");

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task BuscarDominiosPorNombreAsync_ConNombreValido_DebeRetornarDominios()
        {
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio Test 1" },
                new Dominio { IdDominio = 2, Nombre = "Test Dominio 2" }
            };

            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            var resultado = await _service.BuscarDominiosPorNombreAsync("Test");

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task TieneProcesosAsociadosAsync_ConProcesosAsociados_DebeRetornarTrue()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", DominioId = 1 }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);

            var resultado = await _service.TieneProcesosAsociadosAsync(1);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ObtenerDominiosConProcesosAsync_DebeRetornarDominiosConProcesos()
        {
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" }
            };
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", DominioId = 1 }
            };

            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);

            var resultado = await _service.ObtenerDominiosConProcesosAsync();

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerDominiosConSubdominiosAsync_DebeRetornarDominiosConSubdominios()
        {
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" }
            };
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", DominioId = 1 }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ObtenerDominiosConSubdominiosAsync();

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerDominioConDetalleCompletoAsync_ConIdValido_DebeRetornarDetalle()
        {
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio 1" };
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", DominioId = 1 }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ObtenerDominioConDetalleCompletoAsync(1);

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ContarProcesosPorDominioAsync_DebeRetornarConteo()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", DominioId = 1 },
                new Proceso { IdProceso = 2, Codigo = "P2", DominioId = 1 }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);

            var resultado = await _service.ContarProcesosPorDominioAsync(1);

            resultado.Should().Be(2);
        }
    }
}
