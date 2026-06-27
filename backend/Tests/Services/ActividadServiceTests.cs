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
    public class ActividadServiceTests
    {
        private readonly Mock<IActividadRepository> _mockActividadRepo;
        private readonly Mock<ISubdominioRepository> _mockSubdominioRepo;
        private readonly Mock<IProcesoRepository> _mockProcesoRepo;
        private readonly Mock<IDominioRepository> _mockDominioRepo;
        private readonly Mock<IDocumentoRepository> _mockDocumentoRepo;
        private readonly Mock<INotificacionService> _mockNotificacionService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<ActividadService>> _mockLogger;
        private readonly ActividadService _service;

        public ActividadServiceTests()
        {
            _mockActividadRepo = new Mock<IActividadRepository>();
            _mockSubdominioRepo = new Mock<ISubdominioRepository>();
            _mockProcesoRepo = new Mock<IProcesoRepository>();
            _mockDominioRepo = new Mock<IDominioRepository>();
            _mockDocumentoRepo = new Mock<IDocumentoRepository>();
            _mockNotificacionService = new Mock<INotificacionService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockNotificacionService
                .Setup(s => s.CrearNotificacionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .Returns(Task.CompletedTask);
            _mockEmailService
                .Setup(s => s.EnviarCorreoRegistro(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockEmailService
                .Setup(s => s.EnviarCorreoRecuperacion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockEmailService
                .Setup(s => s.EnviarCodigoRecuperacion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockEmailService
                .Setup(s => s.EnviarCodigoTwoFactor(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockEmailService
                .Setup(s => s.EnviarAlertaVencimientoDocumento(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockEmailService
                .Setup(s => s.EnviarAlertaVencimientoActividad(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockLogger = new Mock<ILogger<ActividadService>>();

            _service = new ActividadService(
                _mockActividadRepo.Object,
                _mockSubdominioRepo.Object,
                _mockProcesoRepo.Object,
                _mockDominioRepo.Object,
                _mockNotificacionService.Object,
                _mockEmailService.Object,
                _mockLogger.Object,
                _mockDocumentoRepo.Object
            );
        }

        [Fact]
        public async Task ObtenerActividadPorIdAsync_ConDocumentoVencido_DebeRetornarFlagTrue()
        {
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1, Implementable = "Sí" };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 };
            var documentos = new List<Documento>
            {
                new Documento { IdDocumento = 10, FechaVencimiento = DateTime.UtcNow.Date.AddDays(-1), RolEnActividad = "Principal" }
            };

            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockDocumentoRepo.Setup(r => r.ObtenerPorActividadId(1)).ReturnsAsync(documentos);

            var resultado = await _service.ObtenerActividadPorIdAsync(1);

            resultado.Should().NotBeNull();
            var flag = resultado!.GetType().GetProperty("tieneDocumentosVencidos")?.GetValue(resultado);
            flag.Should().Be(true);
        }

        [Fact]
        public async Task ObtenerTodasLasActividadesAsync_SinDocumentosVencidos_DebeRetornarFlagFalse()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, Implementable = "Sí" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 }
            };
            var documentos = new List<Documento>
            {
                new Documento { IdDocumento = 11, FechaVencimiento = DateTime.UtcNow.Date.AddDays(2) }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockDocumentoRepo.Setup(r => r.ObtenerPorActividadId(1)).ReturnsAsync(documentos);

            var resultado = await _service.ObtenerTodasLasActividadesAsync();

            resultado.Should().HaveCount(1);
            var item = resultado.Single();
            var flag = item.GetType().GetProperty("tieneDocumentosVencidos")?.GetValue(item);
            flag.Should().Be(false);
        }

        [Fact]
        public async Task ObtenerTodasLasActividadesAsync_DebeRetornarTodasLasActividades()
        {
            // Arrange
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, Implementable = "Sí" },
                new Actividad { IdActividad = 2, Nombre = "Act 2", SubdominioId = 2, Implementable = "No" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", ProcesoId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            // Act
            var resultado = await _service.ObtenerTodasLasActividadesAsync();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Should().HaveCount(2);
            _mockActividadRepo.Verify(r => r.ObtenerTodos(), Times.Once);
        }

        [Fact]
        public async Task CrearActividadAsync_ConDatosValidos_DebeCrearActividad()
        {
            // Arrange
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 };
            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(new List<Actividad>());
            _mockActividadRepo.Setup(r => r.Agregar(It.IsAny<Actividad>())).ReturnsAsync(new Actividad { IdActividad = 1 });
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.CrearActividadAsync("Nueva Actividad", "Sí", 1, 1);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockActividadRepo.Verify(r => r.Agregar(It.IsAny<Actividad>()), Times.Once);
        }

        [Fact]
        public async Task CrearActividadAsync_ConNombreVacio_DebeRetornarError()
        {
            // Arrange & Act
            var resultado = await _service.CrearActividadAsync("", "Sí", 1, 1);

            // Assert
            resultado.Should().Contain("Error");
            _mockActividadRepo.Verify(r => r.Agregar(It.IsAny<Actividad>()), Times.Never);
        }

        [Fact]
        public async Task ActualizarPorcentajeAvanceAsync_ConPorcentajeValido_DebeActualizar()
        {
            // Arrange
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1, PorcentajeAvance = 0 };
            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.Actualizar(It.IsAny<Actividad>())).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.ActualizarPorcentajeAvanceAsync(1, 50);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockActividadRepo.Verify(r => r.Actualizar(It.Is<Actividad>(a => a.PorcentajeAvance == 50)), Times.Once);
        }

        [Fact]
        public async Task EliminarActividadAsync_ConIdValido_DebeEliminarActividad()
        {
            // Arrange
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1 };
            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.Eliminar(1)).ReturnsAsync(true);
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.EliminarActividadAsync(1);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockActividadRepo.Verify(r => r.Eliminar(1), Times.Once);
        }

        [Fact]
        public async Task ObtenerActividadPorIdAsync_ConIdValido_DebeRetornarActividad()
        {
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1, Implementable = "Sí" };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 };

            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);

            var resultado = await _service.ObtenerActividadPorIdAsync(1);

            resultado.Should().NotBeNull();
            _mockActividadRepo.Verify(r => r.ObtenerPorId(1), Times.Once);
        }

        [Fact]
        public async Task ActualizarActividadAsync_ConDatosValidos_DebeActualizar()
        {
            var actividad = new Actividad { IdActividad = 1, Nombre = "Old", SubdominioId = 1, Implementable = "Sí" };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 };

            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(new List<Actividad> { actividad });
            _mockActividadRepo.Setup(r => r.Actualizar(It.IsAny<Actividad>())).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarActividadAsync(1, "Updated", "No", 2, 1);

            resultado.Should().Contain("correctamente");
            _mockActividadRepo.Verify(r => r.Actualizar(It.IsAny<Actividad>()), Times.Once);
        }

        [Fact]
        public async Task ExisteActividadPorNombreYSubdominioAsync_ConActividadExistente_DebeRetornarTrue()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1, Implementable = "Sí" }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ExisteActividadPorNombreYSubdominioAsync("Test", 1);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task BuscarActividadesPorNombreAsync_ConNombreValido_DebeRetornarActividades()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad Test", SubdominioId = 1, Implementable = "Sí" },
                new Actividad { IdActividad = 2, Nombre = "Test Actividad", SubdominioId = 1, Implementable = "No" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.BuscarActividadesPorNombreAsync("Test");

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerActividadesPorSubdominioAsync_DebeRetornarActividades()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, Implementable = "Sí" },
                new Actividad { IdActividad = 2, Nombre = "Act 2", SubdominioId = 1, Implementable = "No" }
            };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);

            var resultado = await _service.ObtenerActividadesPorSubdominioAsync(1);

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerActividadesPorProcesoAsync_DebeRetornarActividades()
        {
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", ProcesoId = 1 }
            };
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, Implementable = "Sí" },
                new Actividad { IdActividad = 2, Nombre = "Act 2", SubdominioId = 2, Implementable = "No" }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ObtenerActividadesPorProcesoAsync(1);

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerActividadesPorDominioAsync_DebeRetornarActividades()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", DominioId = 1 }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, Implementable = "Sí" }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ObtenerActividadesPorDominioAsync(1);

            resultado.Should().HaveCount(1);
        }

        [Fact]
        public async Task ActualizarEstadoImplementacionAsync_ConEstadoValido_DebeActualizar()
        {
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1, EstadoImplementacion = "Pendiente" };

            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.Actualizar(It.IsAny<Actividad>())).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarEstadoImplementacionAsync(1, "En Proceso");

            resultado.Should().Contain("exitosamente");
            _mockActividadRepo.Verify(r => r.Actualizar(It.Is<Actividad>(a => a.EstadoImplementacion == "En Proceso")), Times.Once);
        }

        [Fact]
        public async Task ActualizarFechaCompromisoAsync_ConFechaValida_DebeActualizar()
        {
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1 };
            var fecha = new DateTime(2025, 12, 31);

            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.Actualizar(It.IsAny<Actividad>())).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarFechaCompromisoAsync(1, fecha);

            resultado.Should().Contain("exitosamente");
            _mockActividadRepo.Verify(r => r.Actualizar(It.Is<Actividad>(a => a.FechaCompromiso == fecha)), Times.Once);
        }

        [Fact]
        public async Task ActualizarFechaControlAsync_ConFechaValida_DebeActualizar()
        {
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1 };
            var fecha = new DateTime(2025, 11, 30);

            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.Actualizar(It.IsAny<Actividad>())).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarFechaControlAsync(1, fecha);

            resultado.Should().Contain("exitosamente");
            _mockActividadRepo.Verify(r => r.Actualizar(It.Is<Actividad>(a => a.FechaControl == fecha)), Times.Once);
        }

        [Fact]
        public async Task ActualizarDocumentosAsync_ConDocumentosValidos_DebeActualizar()
        {
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1 };

            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.Actualizar(It.IsAny<Actividad>())).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarDocumentosAsync(1, "documento1.pdf, documento2.pdf");

            resultado.Should().Contain("exitosamente");
            _mockActividadRepo.Verify(r => r.Actualizar(It.IsAny<Actividad>()), Times.Once);
        }

        [Fact]
        public async Task ActualizarObservacionesAsync_ConObservacionesValidas_DebeActualizar()
        {
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1 };

            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.Actualizar(It.IsAny<Actividad>())).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarObservacionesAsync(1, "Observaciones importantes");

            resultado.Should().Contain("exitosamente");
            _mockActividadRepo.Verify(r => r.Actualizar(It.IsAny<Actividad>()), Times.Once);
        }

        [Fact]
        public async Task ObtenerActividadesConDetalleCompletoAsync_DebeRetornarActividadesConDetalle()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, Implementable = "Sí" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P1", DominioId = 1 }
            };
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            var resultado = await _service.ObtenerActividadesConDetalleCompletoAsync();

            resultado.Should().HaveCount(1);
        }

        [Fact]
        public async Task ObtenerActividadConDetalleCompletoAsync_ConIdValido_DebeRetornarDetalle()
        {
            var actividad = new Actividad { IdActividad = 1, Nombre = "Test", SubdominioId = 1, Implementable = "Sí" };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P1", DominioId = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio 1" };

            _mockActividadRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(actividad);
            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);

            var resultado = await _service.ObtenerActividadConDetalleCompletoAsync(1);

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerActividadesPorResponsableAsync_DebeRetornarActividades()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, FuncionariosResponsablesId = 5, Implementable = "Sí" },
                new Actividad { IdActividad = 2, Nombre = "Act 2", SubdominioId = 1, FuncionariosResponsablesId = 5, Implementable = "No" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ObtenerActividadesPorResponsableAsync(5);

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task FiltrarActividadesPorEstadoAsync_ConEstadoValido_DebeRetornarActividades()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, EstadoImplementacion = "Completado", Implementable = "Sí" },
                new Actividad { IdActividad = 2, Nombre = "Act 2", SubdominioId = 1, EstadoImplementacion = "Completado", Implementable = "No" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.FiltrarActividadesPorEstadoAsync("Completado");

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task FiltrarActividadesPorImplementableAsync_ConImplementableValido_DebeRetornarActividades()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, Implementable = "Sí" },
                new Actividad { IdActividad = 2, Nombre = "Act 2", SubdominioId = 1, Implementable = "Sí" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.FiltrarActividadesPorImplementableAsync("Sí");

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerActividadesPorRangoAvanceAsync_ConRangoValido_DebeRetornarActividades()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, PorcentajeAvance = 50, Implementable = "Sí" },
                new Actividad { IdActividad = 2, Nombre = "Act 2", SubdominioId = 1, PorcentajeAvance = 75, Implementable = "No" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ObtenerActividadesPorRangoAvanceAsync(40, 80);

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerActividadesPorFechaCompromisoAsync_ConRangoFechaValido_DebeRetornarActividades()
        {
            var fechaInicio = new DateTime(2025, 1, 1);
            var fechaFin = new DateTime(2025, 12, 31);
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, FechaCompromiso = new DateTime(2025, 6, 15), Implementable = "Sí" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ObtenerActividadesPorFechaCompromisoAsync(fechaInicio, fechaFin);

            resultado.Should().HaveCount(1);
        }

        [Fact]
        public async Task ObtenerActividadesVencidasAsync_DebeRetornarActividadesVencidas()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act Vencida", SubdominioId = 1, FechaCompromiso = DateTime.Now.AddDays(-10), Implementable = "Sí", EstadoImplementacion = "Pendiente" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ObtenerActividadesVencidasAsync();

            resultado.Should().HaveCount(1);
        }

        [Fact]
        public async Task ObtenerActividadesProximasAVencerAsync_DebeRetornarActividadesProximas()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act Proxima", SubdominioId = 1, FechaCompromiso = DateTime.Now.AddDays(5), Implementable = "Sí", EstadoImplementacion = "En Proceso" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ObtenerActividadesProximasAVencerAsync(7);

            resultado.Should().HaveCount(1);
        }

        [Fact]
        public async Task ObtenerEstadisticasActividadesAsync_DebeRetornarEstadisticas()
        {
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, EstadoImplementacion = "Completado", PorcentajeAvance = 100, Implementable = "Sí" },
                new Actividad { IdActividad = 2, Nombre = "Act 2", SubdominioId = 1, EstadoImplementacion = "En Proceso", PorcentajeAvance = 50, Implementable = "No" }
            };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ObtenerEstadisticasActividadesAsync();

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerEstadisticasPorSubdominioAsync_ConSubdominioValido_DebeRetornarEstadisticas()
        {
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 };
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Act 1", SubdominioId = 1, EstadoImplementacion = "Completado", PorcentajeAvance = 100, Implementable = "S�" }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockActividadRepo.Setup(r => r.ObtenerPorIdSubdominio(1)).ReturnsAsync(actividades);

            var resultado = await _service.ObtenerEstadisticasPorSubdominioAsync(1);

            resultado.Should().NotBeNull();
        }
    }
}
