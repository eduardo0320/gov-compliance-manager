using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers;
using backend.Services.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace backend.Tests.Controllers
{
    public class ActividadesPorDominioControllerTests
    {
        [Fact]
        public async Task ObtenerActividadesPorDominio_DebeRetornarOkConEstructuraEsperada()
        {
            // Arrange
            var mockActividadService          = new Mock<IActividadService>();
            var mockSubdominioService         = new Mock<ISubdominioService>();
            var mockAuditoriaService          = new Mock<IAuditoriaService>();
            var mockDocumentoService          = new Mock<IDocumentoService>();
            var mockHistorialActividadService = new Mock<IHistorialActividadService>();
            var mockProcesoService            = new Mock<IProcesoService>();
            var mockDominioService            = new Mock<IDominioService>();

            // Datos simulados
            var dominio            = new { id = 1, nombre = "EDM", cantidad_procesos = 1 };
            var proceso            = new { id = 1, dominioId = 1 };
            var subdominio         = new { id = 1, proceso = proceso };
            var actividadPendiente = new { subdominio = subdominio, estado_implementacion = "Pendiente",   fecha_compromiso = System.DateTime.Today.AddDays(1) };
            var actividadVencida   = new { subdominio = subdominio, estado_implementacion = "Pendiente",   fecha_compromiso = System.DateTime.Today.AddDays(-1) };
            var actividadCompletada= new { subdominio = subdominio, estado_implementacion = "Implementado",fecha_compromiso = System.DateTime.Today };

            mockDominioService.Setup(s => s.ObtenerTodosLosDominiosAsync())
                .ReturnsAsync(new List<object> { dominio });
            mockProcesoService.Setup(s => s.ObtenerTodosLosProcesosAsync())
                .ReturnsAsync(new List<object> { proceso });
            mockSubdominioService.Setup(s => s.ObtenerTodosLosSubdominiosAsync())
                .ReturnsAsync(new List<object> { subdominio });
            mockActividadService.Setup(s => s.ObtenerTodasLasActividadesAsync())
                .ReturnsAsync(new List<object> { actividadPendiente, actividadVencida, actividadCompletada });

            var controller = new ActividadesController(
                mockActividadService.Object,
                mockSubdominioService.Object,
                mockAuditoriaService.Object,
                mockDocumentoService.Object,
                mockHistorialActividadService.Object
            );

            // Act
            var result = await controller.ObtenerActividadesPorDominio(
                mockProcesoService.Object,
                mockDominioService.Object,
                CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();

            // Corrección CS8602 / CS8604: usar ! para indicar al compilador que ya validamos con Should()
            var okResult = (result.Result as OkObjectResult)!;
            okResult.Should().NotBeNull();

            var value = (okResult.Value as IEnumerable<object>)?.ToList();
            value.Should().NotBeNullOrEmpty();
            value.Should().HaveCount(1);

            var dominioObj = value!.First();
            dominioObj.Should().NotBeNull();
        }
    }
}