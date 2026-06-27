using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using backend.Controllers;
using backend.Services.Interfaces;

namespace backend.Tests.Controllers
{
    public class TransparenciaControllerTests
    {
        private readonly Mock<ITransparenciaService> _mockTransparenciaService;
        private readonly Mock<ILogger<TransparenciaController>> _mockLogger;
        private readonly TransparenciaController _controller;

        public TransparenciaControllerTests()
        {
            _mockTransparenciaService = new Mock<ITransparenciaService>();
            _mockLogger               = new Mock<ILogger<TransparenciaController>>();

            _controller = new TransparenciaController(
                _mockTransparenciaService.Object,
                _mockLogger.Object);
        }

        // ──────────────────────────────────────────────
        //  ObtenerDocumentosPublicos
        // ──────────────────────────────────────────────

        [Fact]
        public async Task ObtenerDocumentosPublicos_DebeRetornarOkConResumen()
        {
            // Arrange
            var resumen = new
            {
                totalDocumentos      = 3,
                totalDominios        = 1,
                dominios             = new List<object> { new { id = 1, nombre = "APO", totalDocumentos = 3 } },
                ultimaActualizacion  = DateTime.UtcNow
            };

            _mockTransparenciaService
                .Setup(s => s.ObtenerDocumentosPublicosAsync())
                .ReturnsAsync(resumen);

            // Act
            var result = await _controller.ObtenerDocumentosPublicos();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(resumen);
        }

        [Fact]
        public async Task ObtenerDocumentosPublicos_CuandoNoHayDocumentos_DebeRetornarOkConListaVacia()
        {
            // Arrange
            var resumenVacio = new
            {
                totalDocumentos     = 0,
                totalDominios       = 0,
                dominios            = new List<object>(),
                ultimaActualizacion = DateTime.UtcNow
            };

            _mockTransparenciaService
                .Setup(s => s.ObtenerDocumentosPublicosAsync())
                .ReturnsAsync(resumenVacio);

            // Act
            var result = await _controller.ObtenerDocumentosPublicos();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(resumenVacio);
        }

        [Fact]
        public async Task ObtenerDocumentosPublicos_CuandoOcurreError_DebeRetornar500()
        {
            // Arrange
            _mockTransparenciaService
                .Setup(s => s.ObtenerDocumentosPublicosAsync())
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.ObtenerDocumentosPublicos();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ObtenerDocumentosPublicos_DebeInvocarServicioUnaVez()
        {
            // Arrange
            _mockTransparenciaService
                .Setup(s => s.ObtenerDocumentosPublicosAsync())
                .ReturnsAsync(new { totalDocumentos = 0, dominios = new List<object>() });

            // Act
            await _controller.ObtenerDocumentosPublicos();

            // Assert
            _mockTransparenciaService.Verify(s => s.ObtenerDocumentosPublicosAsync(), Times.Once);
        }

        // ──────────────────────────────────────────────
        //  DescargarDocumento
        // ──────────────────────────────────────────────

        [Fact]
        public async Task DescargarDocumento_CuandoEsURL_DebeRetornarOkConUrl()
        {
            // Arrange
            _mockTransparenciaService
                .Setup(s => s.ResolverDescargaPublicaAsync(1))
                .ReturnsAsync(((byte[]?)null, (string?)null, (string?)null, "https://ejemplo.com/doc.pdf"));

            // Act
            var result = await _controller.DescargarDocumento(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DescargarDocumento_CuandoEsArchivo_DebeRetornarFileResult()
        {
            // Arrange
            var bytes = new byte[] { 1, 2, 3, 4 };

            _mockTransparenciaService
                .Setup(s => s.ResolverDescargaPublicaAsync(1))
                .ReturnsAsync((bytes, "application/pdf", "documento.pdf", (string?)null));

            // Act
            var result = await _controller.DescargarDocumento(1);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("application/pdf");
            fileResult.FileDownloadName.Should().Be("documento.pdf");
            fileResult.FileContents.Should().BeEquivalentTo(bytes);
        }

        [Fact]
        public async Task DescargarDocumento_CuandoDocumentoNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            _mockTransparenciaService
                .Setup(s => s.ResolverDescargaPublicaAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Documento no encontrado o no disponible públicamente"));

            // Act
            var result = await _controller.DescargarDocumento(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DescargarDocumento_CuandoDocumentoNoEsPublico_DebeRetornarNotFound()
        {
            // Arrange
            _mockTransparenciaService
                .Setup(s => s.ResolverDescargaPublicaAsync(5))
                .ThrowsAsync(new KeyNotFoundException("Documento no encontrado o no disponible públicamente"));

            // Act
            var result = await _controller.DescargarDocumento(5);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task DescargarDocumento_CuandoOcurreError_DebeRetornar500()
        {
            // Arrange
            _mockTransparenciaService
                .Setup(s => s.ResolverDescargaPublicaAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error interno"));

            // Act
            var result = await _controller.DescargarDocumento(1);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DescargarDocumento_DebeInvocarServicioConIdCorrecto()
        {
            // Arrange
            _mockTransparenciaService
                .Setup(s => s.ResolverDescargaPublicaAsync(7))
                .ReturnsAsync(((byte[]?)null, (string?)null, (string?)null, "https://ejemplo.com/archivo.pdf"));

            // Act
            await _controller.DescargarDocumento(7);

            // Assert
            _mockTransparenciaService.Verify(s => s.ResolverDescargaPublicaAsync(7), Times.Once);
        }
    }
}