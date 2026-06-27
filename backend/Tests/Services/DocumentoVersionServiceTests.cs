using Xunit;
using Moq;
using FluentAssertions;
using backend.Models;
using backend.Services.Implementations;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Tests.Services
{
    public class DocumentoVersionServiceTests
    {
        private readonly Mock<IDocumentoRepository> _mockDocumentoRepo;
        private readonly Mock<IVersionDocumentoRepository> _mockVersionRepo;
        private readonly Mock<IHistorialActividadService> _mockHistorialService;
        private readonly Mock<ILogger<DocumentoVersionService>> _mockLogger;
        private readonly DocumentoVersionService _service;

        public DocumentoVersionServiceTests()
        {
            _mockDocumentoRepo = new Mock<IDocumentoRepository>();
            _mockVersionRepo = new Mock<IVersionDocumentoRepository>();
            _mockHistorialService = new Mock<IHistorialActividadService>();
            _mockLogger = new Mock<ILogger<DocumentoVersionService>>();

            _service = new DocumentoVersionService(
                _mockDocumentoRepo.Object,
                _mockVersionRepo.Object,
                _mockHistorialService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task SubirNuevaVersionAsync_ConArchivoValido_DebeCrearVersionYGuardarNombreOriginal()
        {
            // Arrange
            var documento = new Documento
            {
                IdDocumento = 1,
                Nombre = "Ingeniería 3 - Seguimiento 1 R",
                ActividadId = 1,
                VersionActualId = null
            };

            var nuevaVersion = new VersionDocumento
            {
                IdVersionDocumento = 1,
                DocumentoId = 1,
                NombreArchivoOriginal = "Semana6.pdf",
                NombreArchivoAlmacenado = "doc_1_v1_20260413.pdf",
                FechaCreacion = DateTime.UtcNow,
                TipoVersionamiento = "Mayor"
            };

            _mockDocumentoRepo.Setup(r => r.ObtenerPorId(1))
                .ReturnsAsync(documento);
            _mockVersionRepo.Setup(r => r.Agregar(It.IsAny<VersionDocumento>()))
                .ReturnsAsync(nuevaVersion);
            _mockDocumentoRepo.Setup(r => r.Actualizar(It.IsAny<Documento>()))
                .ReturnsAsync(documento);
            _mockVersionRepo.Setup(r => r.GuardarCambios())
                .ReturnsAsync(1);
            _mockHistorialService.Setup(s => s.RegistrarVersionAnteriorAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var resultado = await _service.SubirNuevaVersionAsync(1, "Semana6.pdf", "doc_1_v1_20260413.pdf", "Mayor");

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockVersionRepo.Verify(r => r.Agregar(It.Is<VersionDocumento>(v =>
                v.NombreArchivoOriginal == "Semana6.pdf" &&
                v.DocumentoId == 1
            )), Times.Once);
            _mockHistorialService.Verify(s => s.RegistrarVersionAnteriorAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SubirNuevaVersionAsync_DebeActualizarVersionActualDelDocumento()
        {
            // Arrange
            var documento = new Documento
            {
                IdDocumento = 1,
                Nombre = "Documento Base",
                ActividadId = 1,
                VersionActualId = null
            };

            var nuevaVersion = new VersionDocumento
            {
                IdVersionDocumento = 2,
                DocumentoId = 1,
                NombreArchivoOriginal = "ReporteFinal.docx",
                NombreArchivoAlmacenado = "doc_1_v2_20260413.docx"
            };

            _mockDocumentoRepo.Setup(r => r.ObtenerPorId(1))
                .ReturnsAsync(documento);
            _mockVersionRepo.Setup(r => r.Agregar(It.IsAny<VersionDocumento>()))
                .ReturnsAsync(nuevaVersion);
            _mockDocumentoRepo.Setup(r => r.Actualizar(It.IsAny<Documento>()))
                .ReturnsAsync(documento);
            _mockVersionRepo.Setup(r => r.GuardarCambios())
                .ReturnsAsync(1);
            _mockHistorialService.Setup(s => s.RegistrarVersionAnteriorAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            await _service.SubirNuevaVersionAsync(1, "ReporteFinal.docx", "doc_1_v2_20260413.docx", "Menor");

            // Assert
            _mockDocumentoRepo.Verify(r => r.Actualizar(It.Is<Documento>(d =>
                d.VersionActualId == 2
            )), Times.Once);
        }

        [Fact]
        public async Task ObtenerVersionesDocumentoAsync_DebeRetornarTodasLasVersionesOrdenadasPorFecha()
        {
            // Arrange
            var versiones = new List<VersionDocumento>
            {
                new VersionDocumento
                {
                    IdVersionDocumento = 1,
                    DocumentoId = 1,
                    NombreArchivoOriginal = "v1.pdf",
                    FechaCreacion = new DateTime(2026, 4, 10),
                    TipoVersionamiento = "Mayor"
                },
                new VersionDocumento
                {
                    IdVersionDocumento = 2,
                    DocumentoId = 1,
                    NombreArchivoOriginal = "v2.pdf",
                    FechaCreacion = new DateTime(2026, 4, 13),
                    TipoVersionamiento = "Menor"
                }
            };

            _mockVersionRepo.Setup(r => r.ObtenerPorDocumentoId(1))
                .ReturnsAsync(versiones);

            // Act
            var resultado = await _service.ObtenerVersionesDocumentoAsync(1);

            // Assert
            resultado.Should().HaveCount(2);
            resultado.Should().BeInDescendingOrder(v => v.FechaCreacion);
            resultado.First().NombreArchivoOriginal.Should().Be("v2.pdf");
        }

        [Fact]
        public async Task ObtenerVersionesDocumentoAsync_DebeRetornarListaVacia_SiNoHayVersiones()
        {
            // Arrange
            _mockVersionRepo.Setup(r => r.ObtenerPorDocumentoId(999))
                .ReturnsAsync(new List<VersionDocumento>());

            // Act
            var resultado = await _service.ObtenerVersionesDocumentoAsync(999);

            // Assert
            resultado.Should().BeEmpty();
        }

        [Fact]
        public async Task ObtenerVersionActualAsync_DebeRetornarUltimaVersion()
        {
            // Arrange
            var versionActual = new VersionDocumento
            {
                IdVersionDocumento = 5,
                DocumentoId = 1,
                NombreArchivoOriginal = "Semana6.pdf",
                NombreArchivoAlmacenado = "doc_1_v5_20260413.pdf",
                FechaCreacion = DateTime.UtcNow
            };

            _mockVersionRepo.Setup(r => r.ObtenerPorId(5))
                .ReturnsAsync(versionActual);

            // Act
            var resultado = await _service.ObtenerVersionActualAsync(5);

            // Assert
            resultado.Should().NotBeNull();
            resultado!.NombreArchivoOriginal.Should().Be("Semana6.pdf");
        }

        [Fact]
        public async Task EliminarVersionAsync_ConVersionNoActual_DebeEliminar()
        {
            // Arrange
            var version = new VersionDocumento
            {
                IdVersionDocumento = 1,
                DocumentoId = 1,
                NombreArchivoOriginal = "vieja.pdf"
            };

            var documento = new Documento
            {
                IdDocumento = 1,
                VersionActualId = 3
            };

            _mockVersionRepo.Setup(r => r.ObtenerPorId(1))
                .ReturnsAsync(version);
            _mockDocumentoRepo.Setup(r => r.ObtenerPorId(1))
                .ReturnsAsync(documento);
            _mockVersionRepo.Setup(r => r.Eliminar(1))
                .ReturnsAsync(true);
            _mockVersionRepo.Setup(r => r.GuardarCambios())
                .ReturnsAsync(1);

            // Act
            var resultado = await _service.EliminarVersionAsync(1);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockVersionRepo.Verify(r => r.Eliminar(1), Times.Once);
        }

        [Fact]
        public async Task EliminarVersionAsync_ConVersionActual_DebeRetornarError()
        {
            // Arrange
            var version = new VersionDocumento
            {
                IdVersionDocumento = 5,
                DocumentoId = 1
            };

            var documento = new Documento
            {
                IdDocumento = 1,
                VersionActualId = 5
            };

            _mockVersionRepo.Setup(r => r.ObtenerPorId(5))
                .ReturnsAsync(version);
            _mockDocumentoRepo.Setup(r => r.ObtenerPorId(1))
                .ReturnsAsync(documento);

            // Act
            var resultado = await _service.EliminarVersionAsync(5);

            // Assert
            resultado.Should().Contain("Error");
            resultado.Should().Contain("actual");
            _mockVersionRepo.Verify(r => r.Eliminar(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ContarVersionesDocumentoAsync_DebeRetornarCantidadDeVersiones()
        {
            // Arrange
            _mockVersionRepo.Setup(r => r.ContarPorDocumentoId(1))
                .ReturnsAsync(5);

            // Act
            var resultado = await _service.ContarVersionesDocumentoAsync(1);

            // Assert
            resultado.Should().Be(5);
        }

        [Fact]
        public async Task ValidarNombreArchivoAsync_ConNombreDuplicado_DebeRetornarFalse()
        {
            // Arrange
            var versiones = new List<VersionDocumento>
            {
                new VersionDocumento { IdVersionDocumento = 1, NombreArchivoOriginal = "Semana6.pdf" }
            };

            _mockVersionRepo.Setup(r => r.ObtenerPorDocumentoId(1))
                .ReturnsAsync(versiones);

            // Act
            var resultado = await _service.ValidarNombreArchivoUnicoAsync(1, "Semana6.pdf");

            // Assert
            resultado.Should().BeFalse();
        }

        [Fact]
        public async Task ValidarNombreArchivoAsync_ConNombreNuevo_DebeRetornarTrue()
        {
            // Arrange
            var versiones = new List<VersionDocumento>
            {
                new VersionDocumento { IdVersionDocumento = 1, NombreArchivoOriginal = "Semana5.pdf" }
            };

            _mockVersionRepo.Setup(r => r.ObtenerPorDocumentoId(1))
                .ReturnsAsync(versiones);

            // Act
            var resultado = await _service.ValidarNombreArchivoUnicoAsync(1, "Semana6.pdf");

            // Assert
            resultado.Should().BeTrue();
        }
    }
}
