using Xunit;
using Moq;
using FluentAssertions;
using backend.Models;
using backend.Services.Implementations;
using backend.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace backend.Tests.Services
{
    public class ProcesoServiceTests
    {
        private readonly Mock<IProcesoRepository> _mockProcesoRepo;
        private readonly Mock<IDominioRepository> _mockDominioRepo;
        private readonly Mock<ISubdominioRepository> _mockSubdominioRepo;
        private readonly Mock<IActividadRepository> _mockActividadRepo;
        private readonly Mock<ILogger<ProcesoService>> _mockLogger;
        private readonly ProcesoService _service;

        public ProcesoServiceTests()
        {
            _mockProcesoRepo = new Mock<IProcesoRepository>();
            _mockDominioRepo = new Mock<IDominioRepository>();
            _mockSubdominioRepo = new Mock<ISubdominioRepository>();
            _mockActividadRepo = new Mock<IActividadRepository>();
            _mockLogger = new Mock<ILogger<ProcesoService>>();

            _service = new ProcesoService(
                _mockProcesoRepo.Object,
                _mockDominioRepo.Object,
                _mockSubdominioRepo.Object,
                _mockActividadRepo.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ObtenerTodosLosProcesosAsync_DebeRetornarTodosLosProcesos()
        {
            // Arrange
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Nombre = "Proceso 1", Codigo = "P001", DominioId = 1 },
                new Proceso { IdProceso = 2, Nombre = "Proceso 2", Codigo = "P002", DominioId = 1 }
            };

            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            // Act
            var resultado = await _service.ObtenerTodosLosProcesosAsync();

            // Assert
            resultado.Should().NotBeNull();
            resultado.Should().HaveCount(2);
            _mockProcesoRepo.Verify(r => r.ObtenerTodos(), Times.Once);
        }

        [Fact]
        public async Task CrearProcesoAsync_ConDatosValidos_DebeCrearProceso()
        {
            // Arrange
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);
            _mockProcesoRepo.Setup(r => r.ObtenerPorCodigo(It.IsAny<string>())).ReturnsAsync((Proceso?)null);
            _mockProcesoRepo.Setup(r => r.Agregar(It.IsAny<Proceso>())).ReturnsAsync(new Proceso { IdProceso = 1 });
            _mockProcesoRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.CrearProcesoAsync("P001", "Nuevo Proceso", "Marco Normativo Test", 1, 1, null);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockProcesoRepo.Verify(r => r.Agregar(It.IsAny<Proceso>()), Times.Once);
        }

        [Fact]
        public async Task CrearProcesoAsync_ConCodigoExistente_DebeRetornarError()
        {
            // Arrange
            var procesoExistente = new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso Existente", DominioId = 1 };
            var procesos = new List<Proceso> { procesoExistente };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };

            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);
            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);

            // Act
            var resultado = await _service.CrearProcesoAsync("P001", "Nuevo Proceso", "Marco Normativo Test", 1, 1, null);

            // Assert
            resultado.Should().Contain("Error");
            resultado.Should().ContainAny("ya existe", "Ya existe");
            _mockProcesoRepo.Verify(r => r.Agregar(It.IsAny<Proceso>()), Times.Never);
        }

        [Fact]
        public async Task ActualizarProcesoAsync_ConDatosValidos_DebeActualizar()
        {
            // Arrange
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso Original", Codigo = "P001", DominioId = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };

            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);
            _mockProcesoRepo.Setup(r => r.Actualizar(It.IsAny<Proceso>())).ReturnsAsync(proceso);
            _mockProcesoRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.ActualizarProcesoAsync(1, "P001", "Proceso Actualizado", "Marco Test", 1, 1, null);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockProcesoRepo.Verify(r => r.Actualizar(It.Is<Proceso>(p => p.Nombre == "Proceso Actualizado")), Times.Once);
        }

        [Fact]
        public async Task EliminarProcesoAsync_ConIdValido_DebeEliminarProceso()
        {
            // Arrange
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso a Eliminar", Codigo = "P001", DominioId = 1 };
            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockProcesoRepo.Setup(r => r.Eliminar(1)).ReturnsAsync(true);
            _mockProcesoRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            // Act
            var resultado = await _service.EliminarProcesoAsync(1);

            // Assert
            resultado.Should().Contain("exitosamente");
            _mockProcesoRepo.Verify(r => r.Eliminar(1), Times.Once);
        }

        [Fact]
        public async Task ObtenerProcesoPorIdAsync_ConIdValido_DebeRetornarProceso()
        {
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso Test", Codigo = "P001", DominioId = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };

            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);

            var resultado = await _service.ObtenerProcesoPorIdAsync(1);

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ExisteProcesoPorCodigoAsync_ConCodigoExistente_DebeRetornarTrue()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso Test", DominioId = 1 }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);

            var resultado = await _service.ExisteProcesoPorCodigoAsync("P001");

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ExisteProcesoPorNombreYDominioAsync_ConProcesoExistente_DebeRetornarTrue()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso Test", DominioId = 1 }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);

            var resultado = await _service.ExisteProcesoPorNombreYDominioAsync("Proceso Test", 1);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ObtenerProcesoPorCodigoAsync_ConCodigoValido_DebeRetornarProceso()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso Test", DominioId = 1 }
            };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);

            var resultado = await _service.ObtenerProcesoPorCodigoAsync("P001");

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task BuscarProcesosPorNombreAsync_ConNombreValido_DebeRetornarProcesos()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso Test 1", DominioId = 1 },
                new Proceso { IdProceso = 2, Codigo = "P002", Nombre = "Test Proceso 2", DominioId = 1 }
            };
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio Test" }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            var resultado = await _service.BuscarProcesosPorNombreAsync("Test");

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerProcesosPorDominioAsync_ConDominioValido_DebeRetornarProcesos()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso 1", DominioId = 1 },
                new Proceso { IdProceso = 2, Codigo = "P002", Nombre = "Proceso 2", DominioId = 1 }
            };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio Test" };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);

            var resultado = await _service.ObtenerProcesosPorDominioAsync(1);

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ActualizarEstadoImplementacionAsync_ConDatosValidos_DebeActualizar()
        {
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso Test", Codigo = "P001", DominioId = 1, EstadoImplementacion = "No" };

            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockProcesoRepo.Setup(r => r.Actualizar(It.IsAny<Proceso>())).ReturnsAsync(proceso);
            _mockProcesoRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarEstadoImplementacionAsync(1, "Sí");

            resultado.Should().Contain("exitosamente");
        }

        [Fact]
        public async Task ActualizarPorcentajeAvanceAsync_ConPorcentajeValido_DebeActualizar()
        {
            var proceso = new Proceso { IdProceso = 1, Nombre = "Proceso Test", Codigo = "P001", DominioId = 1, PorcentajeAvance = 0 };

            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockProcesoRepo.Setup(r => r.Actualizar(It.IsAny<Proceso>())).ReturnsAsync(proceso);
            _mockProcesoRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarPorcentajeAvanceAsync(1, 50);

            resultado.Should().Contain("exitosamente");
        }

        [Fact]
        public async Task TieneSubdominiosAsociadosAsync_ConSubdominiosAsociados_DebeRetornarTrue()
        {
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.TieneSubdominiosAsociadosAsync(1);

            resultado.Should().BeTrue();
        }

        [Fact]
        public async Task ActualizarPorcentajeActividadAsync_ConDatosValidos_DebeActualizar()
        {
            var actividad = new Actividad
            {
                IdActividad = 1,
                Nombre = "Actividad Test",
                SubdominioId = 1,
                PorcentajeAvance = 0,
                FuncionariosResponsablesId = 1
            };
            var subdominio = new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica", ProcesoId = 1 };
            var proceso = new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso", DominioId = 1 };

            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(new List<Actividad> { actividad });
            _mockActividadRepo.Setup(r => r.Actualizar(It.IsAny<Actividad>())).ReturnsAsync(actividad);
            _mockActividadRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);
            _mockSubdominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(subdominio);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(new List<Subdominio> { subdominio });
            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockProcesoRepo.Setup(r => r.Actualizar(It.IsAny<Proceso>())).ReturnsAsync(proceso);
            _mockProcesoRepo.Setup(r => r.GuardarCambios()).ReturnsAsync(1);

            var resultado = await _service.ActualizarPorcentajeActividadAsync(1, 75, 1);

            resultado.Should().Contain("exitosamente");
        }

        [Fact]
        public async Task ObtenerProcesosConSubdominiosAsync_DebeRetornarProcesosConSubdominios()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso 1", DominioId = 1 }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 }
            };
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            var resultado = await _service.ObtenerProcesosConSubdominiosAsync();

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerProcesoConDetalleCompletoAsync_ConIdValido_DebeRetornarDetalle()
        {
            var proceso = new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso 1", DominioId = 1 };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio 1" };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 }
            };
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(proceso);
            _mockDominioRepo.Setup(r => r.ObtenerPorId(1)).ReturnsAsync(dominio);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ObtenerProcesoConDetalleCompletoAsync(1);

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ObtenerProcesosConActividadesAsync_DebeRetornarProcesosConActividades()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso 1", DominioId = 1 }
            };
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" }
            };
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 }
            };
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);
            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ObtenerProcesosConActividadesAsync();

            resultado.Should().NotBeNull();
        }

        [Fact]
        public async Task ContarSubdominiosPorProcesoAsync_DebeRetornarConteo()
        {
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 },
                new Subdominio { IdSubdominio = 2, PracticasGobierno = "Practica 2", ProcesoId = 1 }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);

            var resultado = await _service.ContarSubdominiosPorProcesoAsync(1);

            resultado.Should().Be(2);
        }

        [Fact]
        public async Task ContarActividadesPorProcesoAsync_DebeRetornarConteo()
        {
            var subdominios = new List<Subdominio>
            {
                new Subdominio { IdSubdominio = 1, PracticasGobierno = "Practica 1", ProcesoId = 1 }
            };
            var actividades = new List<Actividad>
            {
                new Actividad { IdActividad = 1, Nombre = "Actividad 1", SubdominioId = 1, FuncionariosResponsablesId = 1 },
                new Actividad { IdActividad = 2, Nombre = "Actividad 2", SubdominioId = 1, FuncionariosResponsablesId = 1 }
            };

            _mockSubdominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(subdominios);
            _mockActividadRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(actividades);

            var resultado = await _service.ContarActividadesPorProcesoAsync(1);

            resultado.Should().Be(2);
        }

        [Fact]
        public async Task FiltrarProcesosPorEstadoAsync_ConEstadoValido_DebeRetornarProcesos()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso 1", DominioId = 1, EstadoImplementacion = "En Progreso" },
                new Proceso { IdProceso = 2, Codigo = "P002", Nombre = "Proceso 2", DominioId = 1, EstadoImplementacion = "En Progreso" }
            };
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            var resultado = await _service.FiltrarProcesosPorEstadoAsync("En Progreso");

            resultado.Should().HaveCount(2);
        }

        [Fact]
        public async Task ObtenerProcesosPorRangoAvanceAsync_ConRangoValido_DebeRetornarProcesos()
        {
            var procesos = new List<Proceso>
            {
                new Proceso { IdProceso = 1, Codigo = "P001", Nombre = "Proceso 1", DominioId = 1, PorcentajeAvance = 50 },
                new Proceso { IdProceso = 2, Codigo = "P002", Nombre = "Proceso 2", DominioId = 1, PorcentajeAvance = 75 }
            };
            var dominios = new List<Dominio>
            {
                new Dominio { IdDominio = 1, Nombre = "Dominio 1" }
            };

            _mockProcesoRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(procesos);
            _mockDominioRepo.Setup(r => r.ObtenerTodos()).ReturnsAsync(dominios);

            var resultado = await _service.ObtenerProcesosPorRangoAvanceAsync(40, 80);

            resultado.Should().HaveCount(2);
        }
    }
}
