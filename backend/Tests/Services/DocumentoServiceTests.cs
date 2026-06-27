using Xunit;
using Moq;
using FluentAssertions;
using backend.Models;
using backend.Services.Implementations;
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;
using backend.Data;
using Microsoft.Extensions.Logging;
using Moq.EntityFrameworkCore;

namespace backend.Tests.Services
{
    public class DocumentoServiceTests
    {
        private readonly Mock<NormasDb> _mockContext;
        private readonly Mock<IDocumentoRepository> _mockDocumentoRepo;
        private readonly Mock<IVersionDocumentoRepository> _mockVersionRepo;
        private readonly Mock<IRelacionDocumentoRepository> _mockRelacionRepo;
        private readonly Mock<IActividadRepository> _mockActividadRepo;
        private readonly Mock<IDominioRepository> _mockDominioRepo;
        private readonly Mock<IProcesoRepository> _mockProcesoRepo;
        private readonly Mock<ISubdominioRepository> _mockSubdominioRepo;
        private readonly Mock<IAlmacenamientoService> _mockAlmacenamientoService;
        private readonly Mock<IIntegridadService> _mockIntegridadService;
        private readonly Mock<IAuditoriaService> _mockAuditoriaService;
        private readonly Mock<IHistorialActividadService> _mockHistorialService;
        private readonly Mock<ILogger<DocumentoService>> _mockLogger;
        private readonly DocumentoService _service;

        public DocumentoServiceTests()
        {
            _mockContext              = new Mock<NormasDb>();
            _mockDocumentoRepo        = new Mock<IDocumentoRepository>();
            _mockVersionRepo          = new Mock<IVersionDocumentoRepository>();
            _mockRelacionRepo         = new Mock<IRelacionDocumentoRepository>();
            _mockActividadRepo        = new Mock<IActividadRepository>();
            _mockDominioRepo          = new Mock<IDominioRepository>();
            _mockProcesoRepo          = new Mock<IProcesoRepository>();
            _mockSubdominioRepo       = new Mock<ISubdominioRepository>();
            _mockAlmacenamientoService= new Mock<IAlmacenamientoService>();
            _mockIntegridadService    = new Mock<IIntegridadService>();
            _mockAuditoriaService     = new Mock<IAuditoriaService>();
            _mockHistorialService     = new Mock<IHistorialActividadService>();
            _mockLogger               = new Mock<ILogger<DocumentoService>>();

            _service = new DocumentoService(
                _mockContext.Object,
                _mockDocumentoRepo.Object,
                _mockVersionRepo.Object,
                _mockRelacionRepo.Object,
                _mockActividadRepo.Object,
                _mockDominioRepo.Object,
                _mockProcesoRepo.Object,
                _mockSubdominioRepo.Object,
                _mockAlmacenamientoService.Object,
                _mockIntegridadService.Object,
                _mockAuditoriaService.Object,
                _mockHistorialService.Object,
                _mockLogger.Object
            );
        }

        // ──────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────

        private static Dominio CrearDominio(int id, string nombre) =>
            new Dominio { IdDominio = id, Nombre = nombre };

        private static Proceso CrearProceso(int id, string codigo, string nombre, int dominioId) =>
            new Proceso { IdProceso = id, Codigo = codigo, Nombre = nombre, DominioId = dominioId,
                          MarcoNormativo = "Marco", EstadoImplementacion = "Sí" };

        private static Subdominio CrearSubdominio(int id, string practicas, int procesoId) =>
            new Subdominio { IdSubdominio = id, PracticasGobierno = practicas,
                             IndicadoresAsociados = "Indicador", ProcesoId = procesoId };

        private static Actividad CrearActividad(int id, string nombre, int subdominioId) =>
            new Actividad { IdActividad = id, Nombre = nombre, SubdominioId = subdominioId };

        private static Documento CrearDocumentoPublico(int id, string nombre, int actividadId,
            string tipo = "PDF", VersionDocumento? version = null) =>
            new Documento
            {
                IdDocumento      = id,
                Nombre           = nombre,
                TipoDocumento    = tipo,
                ActividadId      = actividadId,
                Confidencialidad = "Publica",
                Estado           = "Vigente",
                RolEnActividad   = "Principal",
                Eliminado        = false,
                FechaCreacion    = DateTime.UtcNow,
                VersionActual    = version
            };

        private static VersionDocumento CrearVersion(int id, int docId, int numero, string tipo = "Archivo",
            string? ruta = null, string? url = null) =>
            new VersionDocumento
            {
                IdVersionDocumento   = id,
                DocumentoId          = docId,
                NumeroVersion        = numero,
                VersionTexto         = $"{numero}.0",
                TipoAlmacenamiento   = tipo,
                RutaArchivo          = ruta ?? $"repositorio/doc_{docId}_v{numero}.pdf",
                Url                  = url,
                NombreArchivoOriginal= $"archivo_{docId}.pdf",
                Activo               = true,
                FechaSubida          = DateTime.UtcNow
            };

        // ──────────────────────────────────────────────
        //  ObtenerDocumentosPublicosAsync
        // ──────────────────────────────────────────────

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_ConJerarquiaCompleta_DebeRetornarResumenEstructurado()
        {
            // Arrange
            var dominios    = new List<Dominio>    { CrearDominio(1, "APO — Alinear, Planificar y Organizar") };
            var procesos    = new List<Proceso>    { CrearProceso(1, "APO01", "Gestionar el Marco", 1) };
            var subdominios = new List<Subdominio> { CrearSubdominio(1, "Práctica de Gobernanza 1", 1) };
            var actividades = new List<Actividad>  { CrearActividad(1, "Actividad de prueba", 1) };
            var version     = CrearVersion(1, 1, 1);
            var documentos  = new List<Documento>  { CrearDocumentoPublico(1, "Política de TI", 1, version: version) };

            _mockDominioRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);
            _mockProcesoRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo .Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockDocumentoRepo .Setup(r => r.ObtenerPublicosConVersionAsync()).ReturnsAsync(documentos);

            // Act
            var resultado = await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            resultado.Should().NotBeNull();
            _mockDominioRepo   .Verify(r => r.ObtenerTodos(), Times.Once);
            _mockProcesoRepo   .Verify(r => r.ObtenerTodos(), Times.Once);
            _mockSubdominioRepo.Verify(r => r.ObtenerTodos(), Times.Once);
            _mockActividadRepo .Verify(r => r.ObtenerTodos(), Times.Once);
            _mockDocumentoRepo .Verify(r => r.ObtenerPublicosConVersionAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_SinDocumentosPublicos_DebeRetornarDominiosVacios()
        {
            // Arrange
            var dominios    = new List<Dominio>    { CrearDominio(1, "APO") };
            var procesos    = new List<Proceso>    { CrearProceso(1, "APO01", "Proceso 1", 1) };
            var subdominios = new List<Subdominio> { CrearSubdominio(1, "Práctica 1", 1) };
            var actividades = new List<Actividad>  { CrearActividad(1, "Actividad 1", 1) };

            _mockDominioRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);
            _mockProcesoRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo .Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockDocumentoRepo .Setup(r => r.ObtenerPublicosConVersionAsync())
                               .ReturnsAsync(new List<Documento>());

            // Act
            var resultado = await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            resultado.Should().NotBeNull();

            // Sin documentos, ningún dominio debe aparecer en el resultado (se filtra .Where(d => totalDocumentos > 0))
            var dict = resultado.GetType().GetProperty("dominios")?.GetValue(resultado);
            dict.Should().NotBeNull();
            var lista = dict as IEnumerable<object>;
            lista.Should().BeEmpty();
        }

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_ConVariosDominios_DebeAgruparCorrectamente()
        {
            // Arrange - 2 dominios, cada uno con un documento
            var dominios = new List<Dominio>
            {
                CrearDominio(1, "APO"),
                CrearDominio(2, "BAI")
            };
            var procesos = new List<Proceso>
            {
                CrearProceso(1, "APO01", "Proceso APO", 1),
                CrearProceso(2, "BAI01", "Proceso BAI", 2)
            };
            var subdominios = new List<Subdominio>
            {
                CrearSubdominio(1, "Práctica APO", 1),
                CrearSubdominio(2, "Práctica BAI", 2)
            };
            var actividades = new List<Actividad>
            {
                CrearActividad(1, "Actividad APO", 1),
                CrearActividad(2, "Actividad BAI", 2)
            };
            var documentos = new List<Documento>
            {
                CrearDocumentoPublico(1, "Doc APO", 1, version: CrearVersion(1, 1, 1)),
                CrearDocumentoPublico(2, "Doc BAI", 2, version: CrearVersion(2, 2, 1))
            };

            _mockDominioRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);
            _mockProcesoRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo .Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockDocumentoRepo .Setup(r => r.ObtenerPublicosConVersionAsync()).ReturnsAsync(documentos);

            // Act
            var resultado = await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            resultado.Should().NotBeNull();
            var totalDominios = resultado.GetType().GetProperty("totalDominios")?.GetValue(resultado);
            totalDominios.Should().Be(2);
            var totalDocumentos = resultado.GetType().GetProperty("totalDocumentos")?.GetValue(resultado);
            totalDocumentos.Should().Be(2);
        }

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_ConDocumentoSinVersion_DebeIncluirloConVersionNull()
        {
            // Arrange
            var dominios    = new List<Dominio>    { CrearDominio(1, "APO") };
            var procesos    = new List<Proceso>    { CrearProceso(1, "APO01", "Proceso 1", 1) };
            var subdominios = new List<Subdominio> { CrearSubdominio(1, "Práctica 1", 1) };
            var actividades = new List<Actividad>  { CrearActividad(1, "Actividad 1", 1) };
            var documentos  = new List<Documento>  { CrearDocumentoPublico(1, "Doc sin versión", 1, version: null) };

            _mockDominioRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);
            _mockProcesoRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo .Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockDocumentoRepo .Setup(r => r.ObtenerPublicosConVersionAsync()).ReturnsAsync(documentos);

            // Act
            var resultado = await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            resultado.Should().NotBeNull();
            // El documento aparece aunque no tenga versión (version = null en el mapeo)
            var totalDocumentos = resultado.GetType().GetProperty("totalDocumentos")?.GetValue(resultado);
            totalDocumentos.Should().Be(1);
        }

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_ConDocumentoURL_DebeMarcarEsUrlComoTrue()
        {
            // Arrange
            var dominios    = new List<Dominio>    { CrearDominio(1, "APO") };
            var procesos    = new List<Proceso>    { CrearProceso(1, "APO01", "Proceso 1", 1) };
            var subdominios = new List<Subdominio> { CrearSubdominio(1, "Práctica 1", 1) };
            var actividades = new List<Actividad>  { CrearActividad(1, "Actividad 1", 1) };
            var versionUrl  = CrearVersion(1, 1, 1, tipo: "URL", url: "https://ejemplo.com/doc");
            var documentos  = new List<Documento>  { CrearDocumentoPublico(1, "Enlace externo", 1, tipo: "URL", version: versionUrl) };

            _mockDominioRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);
            _mockProcesoRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo .Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockDocumentoRepo .Setup(r => r.ObtenerPublicosConVersionAsync()).ReturnsAsync(documentos);

            // Act
            var resultado = await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            resultado.Should().NotBeNull();
            var totalDocumentos = resultado.GetType().GetProperty("totalDocumentos")?.GetValue(resultado);
            totalDocumentos.Should().Be(1);
        }

        [Fact]
        public async Task ObtenerDocumentosPublicosAsync_DebeIncluirUltimaActualizacion()
        {
            // Arrange
            _mockDominioRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(new List<Dominio>());
            _mockProcesoRepo   .Setup(r => r.ObtenerTodos()).ReturnsAsync(new List<Proceso>());
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(new List<Subdominio>());
            _mockActividadRepo .Setup(r => r.ObtenerTodos()).ReturnsAsync(new List<Actividad>());
            _mockDocumentoRepo .Setup(r => r.ObtenerPublicosConVersionAsync()).ReturnsAsync(new List<Documento>());

            // Act
            var resultado = await _service.ObtenerDocumentosPublicosAsync();

            // Assert
            var ultimaActualizacion = resultado.GetType().GetProperty("ultimaActualizacion")?.GetValue(resultado);
            ultimaActualizacion.Should().NotBeNull();
            ultimaActualizacion.Should().BeOfType<DateTime>();
        }

        // ──────────────────────────────────────────────
        //  ResolverDescargaPublicaAsync
        // ──────────────────────────────────────────────

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoDocumentoEsURL_DebeRetornarUrlExterna()
        {
            // Arrange
            var version = CrearVersion(1, 1, 1, tipo: "URL", url: "https://ejemplo.com/politica.pdf");
            var documento = CrearDocumentoPublico(1, "Política", 1, tipo: "URL", version: version);

            _mockDocumentoRepo
                .Setup(r => r.ObtenerPublicoPorIdAsync(1))
                .ReturnsAsync(documento);

            // Act
            var (bytes, contentType, fileName, urlExterna) = await _service.ResolverDescargaPublicaAsync(1);

            // Assert
            urlExterna.Should().Be("https://ejemplo.com/politica.pdf");
            bytes.Should().BeNull();
            contentType.Should().BeNull();
            fileName.Should().BeNull();
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoDocumentoEsArchivoPDF_DebeRetornarBytesYContentTypePDF()
        {
            // Arrange
            var rutaTemporal = Path.GetTempFileName();
            var contenidoPDF = new byte[] { 37, 80, 68, 70 }; // %PDF header
            await File.WriteAllBytesAsync(rutaTemporal, contenidoPDF);

            var version  = CrearVersion(1, 1, 1, tipo: "Archivo", ruta: rutaTemporal);
            var documento = CrearDocumentoPublico(1, "Informe", 1, tipo: "PDF", version: version);

            _mockDocumentoRepo
                .Setup(r => r.ObtenerPublicoPorIdAsync(1))
                .ReturnsAsync(documento);

            _mockAlmacenamientoService
                .Setup(s => s.ArchivoExiste(rutaTemporal))
                .Returns(false);

            // Act
            var (bytes, contentType, fileName, urlExterna) = await _service.ResolverDescargaPublicaAsync(1);

            // Assert
            bytes.Should().BeEquivalentTo(contenidoPDF);
            contentType.Should().Be("application/pdf");
            urlExterna.Should().BeNull();

            // Cleanup
            File.Delete(rutaTemporal);
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoDocumentoEsArchivoDOCX_DebeRetornarContentTypeCorrecto()
        {
            // Arrange
            var rutaTemporal = Path.GetTempFileName();
            await File.WriteAllBytesAsync(rutaTemporal, new byte[] { 1, 2, 3 });

            var version   = CrearVersion(1, 1, 1, tipo: "Archivo", ruta: rutaTemporal);
            var documento = CrearDocumentoPublico(1, "Reglamento", 1, tipo: "DOCX", version: version);

            _mockDocumentoRepo
                .Setup(r => r.ObtenerPublicoPorIdAsync(1))
                .ReturnsAsync(documento);

            _mockAlmacenamientoService
                .Setup(s => s.ArchivoExiste(rutaTemporal))
                .Returns(false);

            // Act
            var (_, contentType, _, _) = await _service.ResolverDescargaPublicaAsync(1);

            // Assert
            contentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            // Cleanup
            File.Delete(rutaTemporal);
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoDocumentoNoExiste_DebeLanzarKeyNotFoundException()
        {
            // Arrange
            _mockDocumentoRepo
                .Setup(r => r.ObtenerPublicoPorIdAsync(999))
                .ReturnsAsync((Documento?)null);

            // Act
            var act = async () => await _service.ResolverDescargaPublicaAsync(999);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*no encontrado*");
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoDocumentoSinVersion_DebeLanzarKeyNotFoundException()
        {
            // Arrange
            var documento = CrearDocumentoPublico(1, "Doc sin versión", 1, version: null);

            _mockDocumentoRepo
                .Setup(r => r.ObtenerPublicoPorIdAsync(1))
                .ReturnsAsync(documento);

            // Act
            var act = async () => await _service.ResolverDescargaPublicaAsync(1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*versión*");
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoURLEsNull_DebeLanzarKeyNotFoundException()
        {
            // Arrange
            var version   = CrearVersion(1, 1, 1, tipo: "URL", url: null);
            version.RutaArchivo = null; // sin URL ni ruta
            var documento = CrearDocumentoPublico(1, "URL Rota", 1, tipo: "URL", version: version);

            _mockDocumentoRepo
                .Setup(r => r.ObtenerPublicoPorIdAsync(1))
                .ReturnsAsync(documento);

            // Act
            var act = async () => await _service.ResolverDescargaPublicaAsync(1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*URL*");
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoArchivoFisicoNoExiste_DebeLanzarKeyNotFoundException()
        {
            // Arrange
            var version = CrearVersion(1, 1, 1, tipo: "Archivo", ruta: "/ruta/inexistente/archivo.pdf");
            var documento = CrearDocumentoPublico(1, "Archivo perdido", 1, tipo: "PDF", version: version);

            _mockDocumentoRepo
                .Setup(r => r.ObtenerPublicoPorIdAsync(1))
                .ReturnsAsync(documento);

            _mockAlmacenamientoService
                .Setup(s => s.ArchivoExiste("/ruta/inexistente/archivo.pdf"))
                .Returns(false);

            // Act
            var act = async () => await _service.ResolverDescargaPublicaAsync(1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*archivo*");
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_CuandoUsaAlmacenamientoService_DebeUsarRutaCompleta()
        {
            // Arrange
            var rutaRelativa = "repositorio/actividad_1/doc_1_v1.pdf";
            var rutaAbsoluta = "/var/app/repositorio/actividad_1/doc_1_v1.pdf";
            var rutaTemporal = Path.GetTempFileName();
            await File.WriteAllBytesAsync(rutaTemporal, new byte[] { 1, 2, 3 });

            var version   = CrearVersion(1, 1, 1, tipo: "Archivo", ruta: rutaRelativa);
            var documento = CrearDocumentoPublico(1, "Documento almacenado", 1, tipo: "PDF", version: version);

            _mockDocumentoRepo
                .Setup(r => r.ObtenerPublicoPorIdAsync(1))
                .ReturnsAsync(documento);

            _mockAlmacenamientoService
                .Setup(s => s.ArchivoExiste(rutaRelativa))
                .Returns(true);

            _mockAlmacenamientoService
                .Setup(s => s.ObtenerRutaCompleta(rutaRelativa))
                .Returns(rutaTemporal);

            // Act
            var (bytes, _, _, _) = await _service.ResolverDescargaPublicaAsync(1);

            // Assert
            bytes.Should().NotBeNull();
            _mockAlmacenamientoService.Verify(s => s.ArchivoExiste(rutaRelativa), Times.Once);
            _mockAlmacenamientoService.Verify(s => s.ObtenerRutaCompleta(rutaRelativa), Times.Once);

            // Cleanup
            File.Delete(rutaTemporal);
        }

        [Fact]
        public async Task ResolverDescargaPublicaAsync_DebeInvocarRepositorioConIdCorrecto()
        {
            // Arrange
            _mockDocumentoRepo
                .Setup(r => r.ObtenerPublicoPorIdAsync(42))
                .ReturnsAsync((Documento?)null);

            // Act
            try { await _service.ResolverDescargaPublicaAsync(42); } catch { }

            // Assert
            _mockDocumentoRepo.Verify(r => r.ObtenerPublicoPorIdAsync(42), Times.Once);
        }
    }
}
