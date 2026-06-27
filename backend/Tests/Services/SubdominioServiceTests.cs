using Xunit;
using Moq;
using FluentAssertions;
using backend.Models;
using backend.Services.Implementations;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Tests.Services
{
    public class SubdominioServiceTests
    {
        private readonly Mock<ISubdominioRepository> _mockSubdominioRepo;
        private readonly Mock<IProcesoRepository> _mockProcesoRepo;
        private readonly Mock<IDominioRepository> _mockDominioRepo;
        private readonly Mock<IActividadRepository> _mockActividadRepo;
        private readonly Mock<ILogger<SubdominioService>> _mockLogger;
        private readonly SubdominioService _service;

        public SubdominioServiceTests()
        {
            _mockSubdominioRepo = new Mock<ISubdominioRepository>();
            _mockProcesoRepo = new Mock<IProcesoRepository>();
            _mockDominioRepo = new Mock<IDominioRepository>();
            _mockActividadRepo = new Mock<IActividadRepository>();
            _mockLogger = new Mock<ILogger<SubdominioService>>();

            _service = new SubdominioService(
                _mockSubdominioRepo.Object,
                _mockProcesoRepo.Object,
                _mockDominioRepo.Object,
                _mockActividadRepo.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ObtenerTodosLosSubdominiosAsync_DebeRetornarTodosLosSubdominios()
        {
            // Arrange
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", ProcesoId = 1 }
            };

            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Nombre = "Proceso 1", Codigo = "P001" }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);

            // Act
            var resultado = await _service.ObtenerTodosLosSubdominiosAsync();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Should().HaveCount(2);
            _mockSubdominioRepo.Verify(r => r.ObtenerTodos(), Times.Once);
        }

        [Fact]
        public async Task CrearSubdominioAsync_ConDatosValidos_DebeCrearSubdominio()
        {
            // Arrange
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso Test", Codigo = "P001" };
            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockSubdominioRepo.Setup(r => r.Agregar(It.IsAny<Subdominio>())).ReturnsAsync(new Subdominio { IdSubdominio = 1 });
            _mockSubdominioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.CrearSubdominioAsync("Nuevas Practicas", "Indicadores test", 1);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockSubdominioRepo.Verify(r => r.Agregar(It.IsAny<Subdominio>()), Times.Once);
        }

        [Fact]
        public async Task CrearSubdominioAsync_ConProcesoInexistente_DebeRetornarError()
        {
            // Arrange
            _mockProcesoRepo.Setup(r => r.ObtenerPorId(999)).ReturnsAsync((Proceso?)null);

            // Act
            var resultado = await _service.CrearSubdominioAsync("Practicas", "Indicadores", 999);

            // Assert
            resultado.Should().Contain("Error");
            resultado.Should().Contain("no existe");
            _mockSubdominioRepo.Verify(r => r.Agregar(It.IsAny<Subdominio>()), Times.Never);
        }

        [Fact]
        public async Task ActualizarSubdominioAsync_ConDatosValidos_DebeActualizar()
        {
            // Arrange
            var subdominio = new Subdominio
            {
                IdSubdominio = 1,
                PracticasGobierno = "Practicas Originales",
                IndicadoresAsociados = "Indicadores Originales",
                ProcesoId = 1
            };
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso Test" };

            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockSubdominioRepo.Setup(r => r.Actualizar(It.IsAny<Subdominio>())).ReturnsAsync(subdominio);
            _mockSubdominioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.ActualizarSubdominioAsync(1, "Practicas Actualizadas", "Indicadores Actualizados", 1);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockSubdominioRepo.Verify(r => r.Actualizar(It.Is<Subdominio>(s => s.PracticasGobierno == "Practicas Actualizadas")), Times.Once);
        }

        [Fact]
        public async Task EliminarSubdominioAsync_ConIdValido_DebeEliminarSubdominio()
        {
            // Arrange
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practicas", ProcesoId = 1 };
            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockSubdominioRepo.Setup(r => r.Eliminar(1)).ReturnsAsync(true);
            _mockSubdominioRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.EliminarSubdominioAsync(1);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockSubdominioRepo.Verify(r => r.Eliminar(1), Times.Once);
        }

        [Fact]
        public async Task ObtenerSubdominiosPorProcesoAsync_DebeRetornarSubdominios()
        {
            // Arrange
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", ProcesoId = 1 }
            };
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso Test", Codigo = "P001", DominioId = 1 };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);

            // Act
            var resultado = await _service.ObtenerSubdominiosPorProcesoAsync(1);

            // Assert
            resultado.Should().NotBeNull();
            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerSubdominioPorIdAsync_ConIdValido_DebeRetornarSubdominio()
        {
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica Test", ProcesoId = 1 };
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso Test", Codigo = "P001", DominioId = 1 };

            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);

            var resultado = await _service.ObtenerSubdominioPorIdAsync(1);

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ExisteSubdominioPorPracticasYProcesoAsync_ConSubdominioExistente_DebeRetornarTrue()
        {
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica Test", ProcesoId = 1 }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ExisteSubdominioPorPracticasYProcesoAsync("Practica Test", 1);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task BuscarSubdominiosPorPracticasAsync_ConPracticasValidas_DebeRetornarSubdominios()
        {
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica Test 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Test Practica 2", ProcesoId = 1 }
            };
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Nombre = "Proceso 1", Codigo = "P001", DominioId = 1 }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);

            var resultado = await _service.BuscarSubdominiosPorPracticasAsync("Test");

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task TieneActividadesAsociadasAsync_ConActividadesAsociadas_DebeRetornarTrue()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.TieneActividadesAsociadasAsync(1);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ObtenerSubdominiosConActividadesAsync_DebeRetornarSubdominiosConActividades()
        {
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 }
            };
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Nombre = "Proceso 1", Codigo = "P001", DominioId = 1 }
            };
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ObtenerSubdominiosConActividadesAsync();

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerSubdominioConDetalleCompletoAsync_ConIdValido_DebeRetornarDetalle()
        {
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 };
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso 1", Codigo = "P001", DominioId = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio 1" };
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);
            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ObtenerSubdominioConDetalleCompletoAsync(1);

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerSubdominiosConProcesoYDominioAsync_DebeRetornarSubdominiosConRelaciones()
        {
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 }
            };
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Nombre = "Proceso 1", Codigo = "P001", DominioId = 1 }
            };
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            var resultado = await _service.ObtenerSubdominiosConProcesoYDominioAsync();

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ContarActividadesPorSubdominioAsync_DebeRetornarConteo()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "Actividad 2", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ContarActividadesPorSubdominioAsync(1);

            resultado.Should().Be(2);
        }

        [Fact]
        public async Task FiltrarSubdominiosPorIndicadoresAsync_ConIndicadoresValidos_DebeRetornarSubdominios()
        {
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", IndicadoresAsociados = "Indicador Test", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", IndicadoresAsociados = "Test Indicador", ProcesoId = 1 }
            };
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Nombre = "Proceso 1", Codigo = "P001", DominioId = 1 }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);

            var resultado = await _service.FiltrarSubdominiosPorIndicadoresAsync("Test");

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerSubdominiosPorDominioAsync_ConDominioValido_DebeRetornarSubdominios()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Nombre = "Proceso 1", Codigo = "P001", DominioId = 1 },
                new Proceso { IdProceso = 2, Nombre = "Proceso 2", Codigo = "P002", DominioId = 1 }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", ProcesoId = 2 }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ObtenerSubdominiosPorDominioAsync(1);

            resultado.Should().HaveCount(2);
        }
    }
}
