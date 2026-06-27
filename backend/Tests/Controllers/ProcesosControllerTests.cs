using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using backend.Controllers;
using backend.Services.Interfaces;
using Backend.Dtos;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class ProcesosControllerTests
    {
        private readonly Mock<IProcesoService> _mockProcesoService;
        private readonly Mock<IDominioService> _mockDominioService;
        private readonly Mock<ISubdominioService> _mockSubdominioService;
        private readonly ProcesosController _controller;

        public ProcesosControllerTests()
        {
            _mockProcesoService = new Mock<IProcesoService>();
            _mockDominioService = new Mock<IDominioService>();
            _mockSubdominioService = new Mock<ISubdominioService>();
            _controller = new ProcesosController(
                _mockProcesoService.Object,
                _mockDominioService.Object,
                _mockSubdominioService.Object);
        }

        [Fact]
        public async Task CrearProceso_ConDatosValidos_DebeRetornarCreated()
        {
            // Arrange
            var request = new CrearProcesoRequest 
            { 
                Codigo = "EDM01",
                Nombre = "Proceso Test",
                MarcoNormativo = "ISO 27001",
                DominioId = 1,
                PrioridadImplementacion = 1
            };
            var procesoCreado = new { id_Proceso = 1, codigo = "EDM01", nombre = "Proceso Test" };
            
            _mockProcesoService.Setup(s => s.CrearProcesoAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync("Proceso creado exitosamente: 1");
            
            _mockProcesoService.Setup(s => s.ObtenerProcesoPorIdAsync(1))
                .ReturnsAsync(procesoCreado);

            // Mock Claims
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CrearProceso(request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult!.Value.Should().Be(procesoCreado);
        }

        [Fact]
        public async Task CrearProceso_SinCodigo_DebeRetornarBadRequest()
        {
            // Arrange
            var request = new CrearProcesoRequest { Codigo = "", Nombre = "Test" };

            // Act
            var result = await _controller.CrearProceso(request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CrearProceso_SinNombre_DebeRetornarBadRequest()
        {
            // Arrange
            var request = new CrearProcesoRequest { Codigo = "EDM01", Nombre = "" };

            // Act
            var result = await _controller.CrearProceso(request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CrearProceso_CuandoDominioNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            var request = new CrearProcesoRequest 
            { 
                Codigo = "EDM01",
                Nombre = "Proceso Test",
                DominioId = 999
            };
            
            _mockProcesoService.Setup(s => s.CrearProcesoAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync("Error: Dominio no encontrado");

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CrearProceso(request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CrearProceso_CuandoYaExiste_DebeRetornarConflict()
        {
            // Arrange
            var request = new CrearProcesoRequest 
            { 
                Codigo = "EDM01",
                Nombre = "Proceso Existente",
                DominioId = 1
            };
            
            _mockProcesoService.Setup(s => s.CrearProcesoAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync("Error: Ya existe un proceso");

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CrearProceso(request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task CrearProceso_ConSubdominios_DebeCrearProcesoySubdominios()
        {
            // Arrange
            var request = new CrearProcesoRequest 
            { 
                Codigo = "EDM01",
                Nombre = "Proceso Test",
                DominioId = 1,
                Subdominios = new List<CrearSubdominioDto>
                {
                    new CrearSubdominioDto 
                    { 
                        PracticasGobierno = "Practica 1",
                        IndicadoresAsociados = "Indicador 1"
                    }
                }
            };
            var procesoCreado = new { id_Proceso = 1, codigo = "EDM01" };
            
            _mockProcesoService.Setup(s => s.CrearProcesoAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync("Proceso creado exitosamente: 1");
            
            _mockProcesoService.Setup(s => s.ObtenerProcesoPorIdAsync(1))
                .ReturnsAsync(procesoCreado);
            
            _mockSubdominioService.Setup(s => s.CrearSubdominioAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync("Subdominio creado: 1");

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.CrearProceso(request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            _mockSubdominioService.Verify(s => s.CrearSubdominioAsync(
                "Practica 1", "Indicador 1", 1), Times.Once);
        }

        [Fact]
        public async Task BuscarProcesos_ConQuery_DebeRetornarOkConResultados()
        {
            // Arrange
            var resultados = new List<object>
            {
                new { id_Proceso = 1, codigo = "EDM01", nombre = "Proceso 1" },
                new { id_Proceso = 2, codigo = "EDM02", nombre = "Proceso 2" }
            };
            
            _mockProcesoService.Setup(s => s.BuscarProcesosPorNombreAsync("test"))
                .ReturnsAsync(resultados);

            // Act
            var result = await _controller.BuscarProcesos("test", CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(resultados);
        }

        [Fact]
        public async Task BuscarProcesos_SinQuery_DebeRetornarBadRequest()
        {
            // Act
            var result = await _controller.BuscarProcesos("", CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task BuscarProcesos_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockProcesoService.Setup(s => s.BuscarProcesosPorNombreAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.BuscarProcesos("test", CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task EditarProceso_ConDatosValidos_DebeRetornarOkConProcesoActualizado()
        {
            // Arrange
            var request = new EditarProcesoRequest 
            { 
                Codigo = "EDM01",
                Nombre = "Proceso Actualizado",
                MarcoNormativo = "ISO 27001",
                DominioId = 1
            };
            var procesoActualizado = new { id_Proceso = 1, codigo = "EDM01", nombre = "Proceso Actualizado" };
            
            _mockProcesoService.Setup(s => s.ActualizarProcesoAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync("Proceso actualizado exitosamente");
            
            _mockProcesoService.Setup(s => s.ObtenerProcesoPorIdAsync(1))
                .ReturnsAsync(procesoActualizado);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.EditarProceso(1, request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(procesoActualizado);
        }

        [Fact]
        public async Task EditarProceso_CuandoProcesoNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            var request = new EditarProcesoRequest 
            { 
                Codigo = "EDM01",
                Nombre = "Proceso Test",
                DominioId = 1
            };
            
            _mockProcesoService.Setup(s => s.ActualizarProcesoAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync("Error: Proceso no encontrado");

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.EditarProceso(999, request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ObtenerProceso_CuandoExiste_DebeRetornarOkConProceso()
        {
            // Arrange
            var proceso = new { id_Proceso = 1, codigo = "EDM01", nombre = "Proceso 1" };
            
            _mockProcesoService.Setup(s => s.ObtenerProcesoPorIdAsync(1))
                .ReturnsAsync(proceso);

            // Act
            var result = await _controller.ObtenerProceso(1, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(proceso);
        }

        [Fact]
        public async Task ObtenerProceso_CuandoNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            _mockProcesoService.Setup(s => s.ObtenerProcesoPorIdAsync(999))
                .ReturnsAsync((object)null!);

            // Act
            var result = await _controller.ObtenerProceso(999, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ObtenerProceso_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockProcesoService.Setup(s => s.ObtenerProcesoPorIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.ObtenerProceso(1, CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }
    }
}
