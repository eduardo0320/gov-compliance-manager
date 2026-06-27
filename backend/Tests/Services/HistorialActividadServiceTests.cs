using Xunit;
using Moq;
using FluentAssertions;
using backend.Data;
using backend.Models;
using backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace backend.Tests.Services
{
    public class HistorialActividadServiceTests
    {
        private static NormasDb CrearContexto()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new NormasDb(options);
        }

        private static async Task SeedBaseAsync(NormasDb context)
        {
            var rol = new Rol { idRol = 1, nombre = "ADMIN" };
            var usuario = new Usuario
            {
                Id_Usuario = 1,
                cedula = "111",
                nombre = "Usuario Test",
                correo_electronico = "test@test.com",
                contrasena = "hash",
                idRol = 1
            };
            var dominio = new Dominio { IdDominio = 1, Nombre = "Dominio" };
            var proceso = new Proceso
            {
                IdProceso = 1,
                Codigo = "P1",
                Nombre = "Proceso",
                MarcoNormativo = "Marco",
                EstadoImplementacion = "S�",
                DominioId = 1,
                CreadoPorId = 1,
                ModificadoPorId = 1
            };
            var subdominio = new Subdominio
            {
                IdSubdominio = 1,
                PracticasGobierno = "Practica",
                IndicadoresAsociados = "Indicador",
                ProcesoId = 1
            };

            context.Roles.Add(rol);
            context.Usuarios.Add(usuario);
            context.Dominios.Add(dominio);
            context.Procesos.Add(proceso);
            context.Subdominios.Add(subdominio);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task RegistrarVersionAnteriorAsync_DebeGuardarNombreDeVersionActualEnDocumentos()
        {
            using var context = CrearContexto();
            await SeedBaseAsync(context);

            var actividad = new Actividad
            {
                IdActividad = 1,
                Nombre = "Actividad Test",
                SubdominioId = 1,
                FuncionariosResponsablesId = 1,
                Implementable = "S�",
                EstadoImplementacion = "Pendiente"
            };
            context.Actividades.Add(actividad);

            var documento = new Documento
            {
                IdDocumento = 1,
                Nombre = "Ingenieria 3 - Seguimiento 1 R",
                TipoDocumento = "PDF",
                ActividadId = 1,
                Estado = "Borrador",
                Confidencialidad = "Interna",
                RolEnActividad = "Principal",
                CreadoPorId = 1,
                FechaCreacion = DateTime.UtcNow,
                Eliminado = false
            };
            context.Documentos.Add(documento);
            await context.SaveChangesAsync();

            var version = new VersionDocumento
            {
                IdVersionDocumento = 1,
                DocumentoId = 1,
                NumeroVersion = 1,
                VersionTexto = "1.0",
                TipoAlmacenamiento = "Archivo",
                NombreArchivoOriginal = "Semana6.pdf",
                SubidoPorId = 1,
                FechaSubida = DateTime.UtcNow,
                Activo = true
            };
            context.VersionesDocumento.Add(version);
            await context.SaveChangesAsync();

            documento.VersionActualId = 1;
            await context.SaveChangesAsync();

            var logger = new Mock<ILogger<HistorialActividadService>>();
            var service = new HistorialActividadService(context, logger.Object);

            await service.RegistrarVersionAnteriorAsync(1, "Nueva version");

            var historial = await context.HistorialVersionesActividades.FirstAsync();
            historial.Documentos.Should().NotBeNullOrWhiteSpace();
            historial.Documentos!.Should().Contain("Semana6.pdf");
            historial.Documentos.Should().NotContain("Ingenieria 3 - Seguimiento 1 R");
        }

        [Fact]
        public async Task RegistrarVersionAnteriorAsync_SinVersionActual_DebeUsarNombreBase()
        {
            using var context = CrearContexto();
            await SeedBaseAsync(context);

            context.Actividades.Add(new Actividad
            {
                IdActividad = 1,
                Nombre = "Actividad Test",
                SubdominioId = 1,
                FuncionariosResponsablesId = 1,
                Implementable = "S�",
                EstadoImplementacion = "Pendiente"
            });

            context.Documentos.Add(new Documento
            {
                IdDocumento = 1,
                Nombre = "Documento Base",
                TipoDocumento = "PDF",
                ActividadId = 1,
                Estado = "Borrador",
                Confidencialidad = "Interna",
                RolEnActividad = "Anexo",
                CreadoPorId = 1,
                FechaCreacion = DateTime.UtcNow,
                Eliminado = false
            });

            await context.SaveChangesAsync();

            var logger = new Mock<ILogger<HistorialActividadService>>();
            var service = new HistorialActividadService(context, logger.Object);

            await service.RegistrarVersionAnteriorAsync(1, "Snapshot sin version");

            var historial = await context.HistorialVersionesActividades.FirstAsync();
            historial.Documentos.Should().Contain("Documento Base");
        }

        [Fact]
        public async Task ObtenerHistorialPorActividadAsync_DebeRetornarVersionesOrdenDesc()
        {
            using var context = CrearContexto();
            await SeedBaseAsync(context);

            context.HistorialVersionesActividades.AddRange(
                new HistorialVersionActividad
                {
                    IdHistorialActividad = 1,
                    ActividadId = 1,
                    Version = 1,
                    Nombre = "A",
                    Implementable = "S�",
                    EstadoImplementacion = "Pendiente",
                    FuncionariosResponsablesId = 1,
                    FechaRegistro = new DateTime(2026, 4, 10),
                    DescripcionCambios = "v1"
                },
                new HistorialVersionActividad
                {
                    IdHistorialActividad = 2,
                    ActividadId = 1,
                    Version = 2,
                    Nombre = "A",
                    Implementable = "S�",
                    EstadoImplementacion = "Pendiente",
                    FuncionariosResponsablesId = 1,
                    FechaRegistro = new DateTime(2026, 4, 11),
                    DescripcionCambios = "v2"
                }
            );

            await context.SaveChangesAsync();

            var logger = new Mock<ILogger<HistorialActividadService>>();
            var service = new HistorialActividadService(context, logger.Object);

            var resultado = await service.ObtenerHistorialPorActividadAsync(1);
            var json = JsonSerializer.Serialize(resultado);
            using var doc = JsonDocument.Parse(json);
            var arr = doc.RootElement;

            arr.GetArrayLength().Should().Be(2);
            arr[0].GetProperty("version").GetInt32().Should().Be(2);
            arr[1].GetProperty("version").GetInt32().Should().Be(1);
        }
    }
}
