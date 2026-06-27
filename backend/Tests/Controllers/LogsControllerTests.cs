using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers;
using backend.Services.Interfaces;
using backend.Models;

#if false

namespace backend.Tests.Controllers
{
    public class LogsControllerTests
    {
        private readonly Mock<IAuditoriaService> _mockAuditoriaService;
        private readonly LogsController _controller;

        public LogsControllerTests()
        {
            _mockAuditoriaService = new Mock<IAuditoriaService>();
            _controller = new LogsController(_mockAuditoriaService.Object);
        }

        [Fact]
        public async Task ObtenerLogs_SinModulo_DebeRetornarOkConLogs()
        {
            // Arrange
            var logs = new List<Auditoria>
            {
                new Auditoria
                {
                    IdAuditoria = 1,
                    Descripcion = "Log 1",
                    FechaEvento = DateTime.Now,
                    TipoEvento = "INFO",
                    Modulo = "Procesos",
                    Usuario = new Usuario { nombre = "Juan" },
                    DireccionIp = "192.168.1.1"
                },
                new Auditoria
                {
                    IdAuditoria = 2,
                    Descripcion = "Log 2",
                    FechaEvento = DateTime.Now,
                    TipoEvento = "ERROR",
                    Modulo = "Usuarios",
                    Usuario = null,
                    DireccionIp = "192.168.1.2"
                }
            };

            _mockAuditoriaService.Setup(s => s.ObtenerLogsAsync(1, 50))
                .ReturnsAsync(logs);

            // Act
            var result = await _controller.ObtenerLogs(1, 50, null);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as List<LogResponse>;
            response.Should().HaveCount(2);
            response![0].NombreUsuario.Should().Be("Juan");
            response[1].NombreUsuario.Should().Be("Sistema");
        }

        [Fact]
        public async Task ObtenerLogs_ConModulo_DebeRetornarOkConLogsDelModulo()
        {
            // Arrange
            var logs = new List<Auditoria>
            {
                new Auditoria
                {
                    IdAuditoria = 1,
                    Descripcion = "Log Procesos",
                    FechaEvento = DateTime.Now,
                    TipoEvento = "INFO",
                    Modulo = "Procesos",
                    Usuario = new Usuario { nombre = "Ana" }
                }
            };

            _mockAuditoriaService.Setup(s => s.ObtenerLogsPorModuloAsync("Procesos", 1, 50))
                .ReturnsAsync(logs);

            // Act
            var result = await _controller.ObtenerLogs(1, 50, "Procesos");

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as List<LogResponse>;
            response.Should().HaveCount(1);
            response![0].Modulo.Should().Be("Procesos");
        }

        [Fact]
        public async Task ObtenerLogs_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockAuditoriaService.Setup(s => s.ObtenerLogsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.ObtenerLogs(1, 50, null);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public void ObtenerModulos_DebeRetornarListaDeModulos()
        {
            // Act
            var result = _controller.ObtenerModulos();

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var modulos = okResult!.Value as List<string>;
            modulos.Should().Contain("Procesos");
            modulos.Should().Contain("Actividades");
            modulos.Should().Contain("Usuarios");
        }

        [Fact]
        public async Task ObtenerLogsPorUsuario_DebeRetornarOkConLogsDelUsuario()
        {
            // Arrange
            var logs = new List<Auditoria>
            {
                new Auditoria
                {
                    IdAuditoria = 1,
                    Descripcion = "Acci�n del usuario",
                    FechaEvento = DateTime.Now,
                    TipoEvento = "INFO",
                    Modulo = "Usuarios",
                    IdUsuario = 1,
                    Usuario = new Usuario { Id_Usuario = 1, nombre = "Pedro" }
                }
            };

            _mockAuditoriaService.Setup(s => s.ObtenerLogsPorUsuarioAsync(1, 1, 50))
                .ReturnsAsync(logs);

            // Act
            var result = await _controller.ObtenerLogsPorUsuario(1, 1, 50);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as List<LogResponse>;
            response.Should().HaveCount(1);
            response![0].NombreUsuario.Should().Be("Pedro");
        }

        [Fact]
        public async Task ObtenerLogsPorUsuario_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockAuditoriaService.Setup(s => s.ObtenerLogsPorUsuarioAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error al obtener logs"));

            // Act
            var result = await _controller.ObtenerLogsPorUsuario(1, 1, 50);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerEstadisticas_DebeRetornarOkConEstadisticas()
        {
            // Arrange
            var estadisticas = new Dictionary<string, int>
            {
                { "INFO", 100 },
                { "ERROR", 10 }
            };

            var ultimosEventos = new List<Auditoria>
            {
                new Auditoria
                {
                    IdAuditoria = 1,
                    Descripcion = "Evento reciente",
                    FechaEvento = DateTime.Now,
                    TipoEvento = "INFO",
                    Modulo = "Sistema",
                    Usuario = new Usuario { nombre = "Admin" }
                }
            };

            _mockAuditoriaService.Setup(s => s.ObtenerEstadisticasPorTipoEventoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(estadisticas);

            _mockAuditoriaService.Setup(s => s.ObtenerUltimosEventosAsync(10))
                .ReturnsAsync(ultimosEventos);

            // Act
            var result = await _controller.ObtenerEstadisticas(null, null);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerEstadisticas_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockAuditoriaService.Setup(s => s.ObtenerEstadisticasPorTipoEventoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Error al obtener estad�sticas"));

            // Act
            var result = await _controller.ObtenerEstadisticas(null, null);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }
    }
}
#endif
