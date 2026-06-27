using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using backend.Services.Implementations;
using backend.Services.Interfaces;

namespace backend.Tests.Services
{
    public class TransparenciaServiceTests
    {
        private readonly Mock<IDocumentoService> _mockDocumentoService;
        private readonly Mock<ILogger<TransparenciaService>> _mockLogger;
        private readonly TransparenciaService _service;

        public TransparenciaServiceTests()
        {
            _mockDocumentoService = new Mock<IDocumentoService>();
            _mockLogger           = new Mock<ILogger<TransparenciaService>>();

            _service = new TransparenciaService(
                _mockDocumentoService.Object,
                _mockLogger.Object);
        }

        // ──────────────────────────────────────────────
        //  ObtenerDocumentosPublicosAsync
        // ──────────────────────────────────────────────

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_DebeRetornarResumenDelDocumentoService()
        {
            // Arrange
            var resumenEsperado = new
            {
                totalDocumentos = 5,
                totalDominios   = 2,
                dominios        = new List<object>()
            };

            _mockDocumentoService
                .Setup(s => s.ObtenerDocumentosPublicosAsync())
                .ReturnsAsync(resumenEsperado);

            // Act
            var resultado = await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            resultado.Should().Be(resumenEsperado);
        }

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_DebeInvocarDocumentoServiceUnaVez()
        {
            // Arrange
            _mockDocumentoService
                .Setup(s => s.ObtenerDocumentosPublicosAsync())
                .ReturnsAsync(new { totalDocumentos = 0 });

            // Act
            await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            _mockDocumentoService.Verify(s => s.ObtenerDocumentosPublicosAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_CuandoDocumentoServiceLanzaExcepcion_DebePropagar()
        {
            // Arrange
            _mockDocumentoService
                .Setup(s => s.ObtenerDocumentosPublicosAsync())
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var act = async () => await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Error de base de datos");
        }

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_CuandoNoHayDocumentos_DebeRetornarResumenVacio()
        {
            // Arrange
            var resumenVacio = new
            {
                totalDocumentos = 0,
                totalDominios   = 0,
                dominios        = new List<object>()
            };

            _mockDocumentoService
                .Setup(s => s.ObtenerDocumentosPublicosAsync())
                .ReturnsAsync(resumenVacio);

            // Act
            var resultado = await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            resultado.Should().Be(resumenVacio);
        }

        // ──────────────────────────────────────────────
        //  ResolverDescargaPublicaAsync
        // ──────────────────────────────────────────────

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoEsArchivo_DebeRetornarBytesYMetadata()
        {
            // Arrange
            var bytes = new byte[] { 1, 2, 3, 4, 5 };

            _mockDocumentoService
                .Setup(s => s.ResolverDescargaPublicaAsync(1))
                .ReturnsAsync((bytes, "application/pdf", "norma.pdf", (string?)null));

            // Act
            var (resultBytes, contentType, fileName, urlExterna) =
                await _service.ResolverDescargaPublicaAsync(1);

            // Assert
            resultBytes.Should().BeEquivalentTo(bytes);
            contentType.Should().Be("application/pdf");
            fileName.Should().Be("norma.pdf");
            urlExterna.Should().BeNull();
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoEsURL_DebeRetornarUrlExterna()
        {
            // Arrange
            var urlEsperada = "https://ejemplo.com/documento.pdf";

            _mockDocumentoService
                .Setup(s => s.ResolverDescargaPublicaAsync(2))
                .ReturnsAsync(((byte[]?)null, (string?)null, (string?)null, urlEsperada));

            // Act
            var (bytes, contentType, fileName, urlExterna) =
                await _service.ResolverDescargaPublicaAsync(2);

            // Assert
            bytes.Should().BeNull();
            contentType.Should().BeNull();
            fileName.Should().BeNull();
            urlExterna.Should().Be(urlEsperada);
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoDocumentoNoExiste_DebePropagarKeyNotFoundException()
        {
            // Arrange
            _mockDocumentoService
                .Setup(s => s.ResolverDescargaPublicaAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Documento no encontrado o no disponible públicamente"));

            // Act
            var act = async () => await _service.ResolverDescargaPublicaAsync(999);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*no encontrado*");
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoDocumentoNoEsPublico_DebePropagarKeyNotFoundException()
        {
            // Arrange
            _mockDocumentoService
                .Setup(s => s.ResolverDescargaPublicaAsync(5))
                .ThrowsAsync(new KeyNotFoundException("Documento no encontrado o no disponible públicamente"));

            // Act
            var act = async () => await _service.ResolverDescargaPublicaAsync(5);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_DebeInvocarDocumentoServiceConIdCorrecto()
        {
            // Arrange
            _mockDocumentoService
                .Setup(s => s.ResolverDescargaPublicaAsync(7))
                .ReturnsAsync(((byte[]?)null, (string?)null, (string?)null, "https://ejemplo.com/doc.pdf"));

            // Act
            await _service.ResolverDescargaPublicaAsync(7);

            // Assert
            _mockDocumentoService.Verify(s => s.ResolverDescargaPublicaAsync(7), Times.Once);
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoOcurreErrorGeneral_DebePropagar()
        {
            // Arrange
            _mockDocumentoService
                .Setup(s => s.ResolverDescargaPublicaAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error de almacenamiento"));

            // Act
            var act = async () => await _service.ResolverDescargaPublicaAsync(1);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Error de almacenamiento");
        }
    }
}