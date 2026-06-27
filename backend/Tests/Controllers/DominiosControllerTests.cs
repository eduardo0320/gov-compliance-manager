using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers;
using backend.Services.Interfaces;
using backend.Models;

namespace backend.Tests.Controllers
{
    public class DominiosControllerTests
    {
        private readonly Mock<IDominioService> _mockDominioService;
        private readonly Mock<IProcesoService> _mockProcesoService;
        private readonly DominiosController _controller;

        public DominiosControllerTests()
        {
            _mockDominioService = new Mock<IDominioService>();
            _mockProcesoService = new Mock<IProcesoService>();
            _controller = new DominiosController(_mockDominioService.Object, _mockProcesoService.Object);
        }

        [Fact]
        public async Task ObtenerTodos_DebeRetornarOkConListaDominios()
        {
            // Arrange
            var dominios = new List<object>
            {
                new { id_Dominio = 1, nombre = "EDM" },
                new { id_Dominio = 2, nombre = "APO" }
            };
            
            _mockDominioService.Setup(s => s.ObtenerTodosLosDominiosAsync())
                .ReturnsAsync(dominios);

            // Act
            var result = await _controller.ObtenerTodos(CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(dominios);
        }

        [Fact]
        public async Task ObtenerTodos_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockDominioService.Setup(s => s.ObtenerTodosLosDominiosAsync())
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.ObtenerTodos(CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerPorId_CuandoExiste_DebeRetornarOkConDominio()
        {
            // Arrange
            var dominio = new { id_Dominio = 1, nombre = "EDM" };
            
            _mockDominioService.Setup(s => s.ObtenerDominioPorIdAsync(1))
                .ReturnsAsync(dominio);

            // Act
            var result = await _controller.ObtenerPorId(1, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(dominio);
        }

        [Fact]
        public async Task ObtenerPorId_CuandoNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            _mockDominioService.Setup(s => s.ObtenerDominioPorIdAsync(999))
                .ReturnsAsync((object)null!);

            // Act
            var result = await _controller.ObtenerPorId(999, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ObtenerPorId_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockDominioService.Setup(s => s.ObtenerDominioPorIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.ObtenerPorId(1, CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerProcesosPorDominio_DebeRetornarOkConProcesos()
        {
            // Arrange
            var procesos = new List<object>
            {
                new { id_Proceso = 1, codigo = "EDM01", nombre = "Proceso 1" },
                new { id_Proceso = 2, codigo = "EDM02", nombre = "Proceso 2" }
            };
            
            _mockProcesoService.Setup(s => s.ObtenerProcesosPorDominioAsync(1))
                .ReturnsAsync(procesos);

            // Act
            var result = await _controller.ObtenerProcesosPorDominio(1, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(procesos);
        }

        [Fact]
        public async Task ObtenerProcesosPorDominio_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockProcesoService.Setup(s => s.ObtenerProcesosPorDominioAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.ObtenerProcesosPorDominio(1, CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerArbol_DebeRetornarOkConDominiosConProcesos()
        {
            // Arrange
            var dominiosConProcesos = new List<object>
            {
                new { id_Dominio = 1, nombre = "EDM", procesos = new List<object>() }
            };
            
            _mockDominioService.Setup(s => s.ObtenerDominiosConProcesosAsync())
                .ReturnsAsync(dominiosConProcesos);

            // Act
            var result = await _controller.ObtenerArbol(CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(dominiosConProcesos);
        }

        [Fact]
        public async Task ObtenerArbol_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockDominioService.Setup(s => s.ObtenerDominiosConProcesosAsync())
                .ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.ObtenerArbol(CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerArbolConSubdominios_DebeRetornarOkConDominiosConSubdominios()
        {
            // Arrange
            var dominiosConSubdominios = new List<object>
            {
                new { id_Dominio = 1, nombre = "EDM", subdominios = new List<object>() }
            };
            
            _mockDominioService.Setup(s => s.ObtenerDominiosConSubdominiosAsync())
                .ReturnsAsync(dominiosConSubdominios);

            // Act
            var result = await _controller.ObtenerArbolConSubdominios(CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(dominiosConSubdominios);
        }

        [Fact]
        public async Task ObtenerArbolConSubdominios_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockDominioService.Setup(s => s.ObtenerDominiosConSubdominiosAsync())
                .ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.ObtenerArbolConSubdominios(CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }
    }
}
