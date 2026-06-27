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
    public class ActividadesControllerTests
    {
        private readonly Mock<IActividadService> _mockActividadService;
        private readonly Mock<ISubdominioService> _mockSubdominioService;
        private readonly Mock<IAuditoriaService> _mockAuditoriaService;
        private readonly Mock<IDocumentoService> _mockDocumentoService;
        private readonly Mock<IHistorialActividadService> _mockHistorialActividadService;
        private readonly ActividadesController _controller;

        public ActividadesControllerTests()
        {
            _mockActividadService = new Mock<IActividadService>();
            _mockSubdominioService = new Mock<ISubdominioService>();
            _mockAuditoriaService = new Mock<IAuditoriaService>();
            _mockDocumentoService = new Mock<IDocumentoService>();
            _mockHistorialActividadService = new Mock<IHistorialActividadService>();
            _mockHistorialActividadService
                .Setup(s => s.RegistrarVersionAnteriorAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(Task.CompletedTask);
            _controller = new ActividadesController(
                _mockActividadService.Object,
                _mockSubdominioService.Object,
                _mockAuditoriaService.Object,
                _mockDocumentoService.Object,
                _mockHistorialActividadService.Object);
        }

        [Fact]
        public async Task Listar_DebeRetornarOkConActividades()
        {
            // Arrange
            var actividades = new List<object>
            {
                new { id_Actividad = 1, nombre = "Actividad 1" },
                new { id_Actividad = 2, nombre = "Actividad 2" }
            };

            _mockActividadService.Setup(s => s.ObtenerActividadesPorSubdominioAsync(1))
                .ReturnsAsync(actividades);

            // Act
            var result = await _controller.Listar(1, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(actividades);
        }

        [Fact]
        public async Task Listar_CuandoSubdominioNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            _mockActividadService.Setup(s => s.ObtenerActividadesPorSubdominioAsync(999))
                .ThrowsAsync(new ArgumentException("Subdominio no encontrado"));

            // Act
            var result = await _controller.Listar(999, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Listar_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockActividadService.Setup(s => s.ObtenerActividadesPorSubdominioAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.Listar(1, CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Crear_ConDatosValidos_DebeRetornarCreated()
        {
            // Arrange
            var request = new CrearActividadRequest { Nombre = "Nueva Actividad", FuncionariosResponsablesId = 1 };
            var actividadCreada = new { id_Actividad = 1, nombre = "Nueva Actividad" };

            _mockActividadService.Setup(s => s.CrearActividadAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync("Actividad creada exitosamente: 1");

            _mockActividadService.Setup(s => s.ObtenerActividadPorIdAsync(1))
                .ReturnsAsync(actividadCreada);

            // Mock Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("rol", "ADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Crear(1, request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult!.Value.Should().Be(actividadCreada);
        }

        [Fact]
        public async Task Crear_SinNombre_DebeRetornarBadRequest()
        {
            // Arrange
            var request = new CrearActividadRequest { Nombre = "", FuncionariosResponsablesId = 1 };

            // Act
            var result = await _controller.Crear(1, request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Crear_CuandoSubdominioNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            var request = new CrearActividadRequest { Nombre = "Nueva Actividad", FuncionariosResponsablesId = 1 };

            _mockActividadService.Setup(s => s.CrearActividadAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync("Error: Subdominio no encontrado");

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.Crear(999, request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Crear_CuandoYaExiste_DebeRetornarConflict()
        {
            // Arrange
            var request = new CrearActividadRequest { Nombre = "Actividad Existente", FuncionariosResponsablesId = 1 };

            _mockActividadService.Setup(s => s.CrearActividadAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync("Error: Ya existe una actividad");

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.Crear(1, request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task Obtener_CuandoExiste_DebeRetornarOkConActividad()
        {
            // Arrange
            var actividad = new { id_Actividad = 1, nombre = "Actividad 1" };

            _mockActividadService.Setup(s => s.ObtenerActividadPorIdAsync(1))
                .ReturnsAsync(actividad);

            // Act
            var result = await _controller.Obtener(1, 1, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(actividad);
        }

        [Fact]
        public async Task Obtener_CuandoNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            _mockActividadService.Setup(s => s.ObtenerActividadPorIdAsync(999))
                .ReturnsAsync((object)null!);

            // Act
            var result = await _controller.Obtener(1, 999, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Actualizar_ConDatosValidos_DebeRetornarOkConActividadActualizada()
        {
            // Arrange
            var request = new ActualizarActividadRequest
            {
                Nombre = "Actividad Actualizada",
                Implementable = "S�",
                FuncionariosResponsablesId = 1
            };
            var actividadActualizada = new { id_Actividad = 1, nombre = "Actividad Actualizada" };

            _mockActividadService.Setup(s => s.ActualizarActividadAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync("Actividad actualizada correctamente");

            _mockActividadService.Setup(s => s.ObtenerActividadPorIdAsync(1))
                .ReturnsAsync(actividadActualizada);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("rol", "ADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.Actualizar(1, 1, request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(actividadActualizada);
        }

        [Fact]
        public async Task Actualizar_ConEstadoImplementacion_DebeActualizarEstado()
        {
            // Arrange
            var request = new ActualizarActividadRequest
            {
                EstadoImplementacion = "En Progreso"
            };
            var actividadActualizada = new { id_Actividad = 1, estadoImplementacion = "En Progreso" };

            _mockActividadService.Setup(s => s.ActualizarEstadoImplementacionAsync(1, "En Progreso"))
                .ReturnsAsync("Estado actualizado");

            _mockActividadService.Setup(s => s.ObtenerActividadPorIdAsync(1))
                .ReturnsAsync(actividadActualizada);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("rol", "ADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await _controller.Actualizar(1, 1, request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Actualizar_CuandoActividadNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            var request = new ActualizarActividadRequest { Nombre = "Nombre" };

            _mockActividadService.Setup(s => s.ActualizarActividadAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync("Error: Actividad no encontrada");

            // Act
            var result = await _controller.Actualizar(1, 999, request, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ObtenerEstadisticas_DebeRetornarOkConEstadisticas()
        {
            // Arrange
            var estadisticas = new
            {
                total = 10,
                completadas = 5,
                enProgreso = 3,
                pendientes = 2
            };

            _mockActividadService.Setup(s => s.ObtenerEstadisticasPorSubdominioAsync(1))
                .ReturnsAsync(estadisticas);

            // Act
            var result = await _controller.ObtenerEstadisticas(1, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().Be(estadisticas);
        }

        [Fact]
        public async Task ObtenerEstadisticas_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockActividadService.Setup(s => s.ObtenerEstadisticasPorSubdominioAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.ObtenerEstadisticas(1, CancellationToken.None);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }
    }
}
