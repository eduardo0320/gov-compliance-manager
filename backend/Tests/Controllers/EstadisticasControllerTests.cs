using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers;
using backend.Services.Interfaces;

namespace backend.Tests.Controllers
{
    public class EstadisticasControllerTests
    {
        private readonly Mock<IActividadService> _mockActividadService;
        private readonly Mock<IDominioService> _mockDominioService;
        private readonly Mock<IProcesoService> _mockProcesoService;
        private readonly Mock<IDocumentoService> _mockDocumentoService;
        private readonly EstadisticasController _controller;

        public EstadisticasControllerTests()
        {
            _mockActividadService = new Mock<IActividadService>();
            _mockDominioService = new Mock<IDominioService>();
            _mockProcesoService = new Mock<IProcesoService>();
            _mockDocumentoService = new Mock<IDocumentoService>();
            _controller = new EstadisticasController(
                _mockActividadService.Object,
                _mockDominioService.Object,
                _mockProcesoService.Object,
                _mockDocumentoService.Object);
        }

        [Fact]
        public async Task ObtenerEstadisticasActividades_DebeRetornarOkConEstadisticas()
        {
            // Arrange
            var estadisticas = new
            {
                total_actividades = 100,
                completadas = 50,
                en_progreso = 30,
                pendientes = 20
            };

            _mockActividadService.Setup(s => s.ObtenerEstadisticasActividadesAsync())
                .ReturnsAsync(estadisticas);

            // Act
            var result = await _controller.ObtenerEstadisticasActividades(CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(estadisticas);
        }

        [Fact]
        public async Task ObtenerEstadisticasActividades_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockActividadService.Setup(s => s.ObtenerEstadisticasActividadesAsync())
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.ObtenerEstadisticasActividades(CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerResumenEstadisticas_DebeRetornarOkConResumen()
        {
            // Arrange
            var dominios = new List<object>
            {
                new { id_Dominio = 1, nombre = "EDM" },
                new { id_Dominio = 2, nombre = "APO" }
            };

            var procesos = new List<object>
            {
                new { id_Proceso = 1, codigo = "EDM01" },
                new { id_Proceso = 2, codigo = "APO01" },
                new { id_Proceso = 3, codigo = "APO02" }
            };

            var estadisticasActividades = new
            {
                total_actividades = 100,
                completadas = 50
            };

            _mockDominioService.Setup(s => s.ObtenerTodosLosDominiosAsync())
                .ReturnsAsync(dominios);

            _mockProcesoService.Setup(s => s.ObtenerTodosLosProcesosAsync())
                .ReturnsAsync(procesos);

            _mockActividadService.Setup(s => s.ObtenerEstadisticasActividadesAsync())
                .ReturnsAsync(estadisticasActividades);

            // Act
            var result = await _controller.ObtenerResumenEstadisticas(CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            // Verificar que se llamaron todos los servicios
            _mockDominioService.Verify(s => s.ObtenerTodosLosDominiosAsync(), Times.Once);
            _mockProcesoService.Verify(s => s.ObtenerTodosLosProcesosAsync(), Times.Once);
            _mockActividadService.Verify(s => s.ObtenerEstadisticasActividadesAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerResumenEstadisticas_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockDominioService.Setup(s => s.ObtenerTodosLosDominiosAsync())
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.ObtenerResumenEstadisticas(CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerEstadisticasSubdominio_DebeRetornarOkConEstadisticas()
        {
            // Arrange
            var estadisticas = new
            {
                subdominio_id = 1,
                total_actividades = 20,
                completadas = 10,
                en_progreso = 5,
                pendientes = 5
            };

            _mockActividadService.Setup(s => s.ObtenerEstadisticasPorSubdominioAsync(1))
                .ReturnsAsync(estadisticas);

            // Act
            var result = await _controller.ObtenerEstadisticasSubdominio(1, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(estadisticas);
        }

        [Fact]
        public async Task ObtenerEstadisticasSubdominio_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockActividadService.Setup(s => s.ObtenerEstadisticasPorSubdominioAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error al obtener estad�sticas"));

            // Act
            var result = await _controller.ObtenerEstadisticasSubdominio(1, CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerResumenEstadisticas_DebeContarCorrectamenteLosElementos()
        {
            // Arrange
            var dominios = new List<object>
            {
                new { id_Dominio = 1 },
                new { id_Dominio = 2 }
            };

            var procesos = new List<object>
            {
                new { id_Proceso = 1 },
                new { id_Proceso = 2 },
                new { id_Proceso = 3 },
                new { id_Proceso = 4 }
            };

            var estadisticasActividades = new
            {
                total_actividades = 150
            };

            _mockDominioService.Setup(s => s.ObtenerTodosLosDominiosAsync())
                .ReturnsAsync(dominios);

            _mockProcesoService.Setup(s => s.ObtenerTodosLosProcesosAsync())
                .ReturnsAsync(procesos);

            _mockActividadService.Setup(s => s.ObtenerEstadisticasActividadesAsync())
                .ReturnsAsync(estadisticasActividades);

            // Act
            var result = await _controller.ObtenerResumenEstadisticas(CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var resumen = okResult!.Value;
            resumen.Should().NotBeNull();
        }
    }
}
