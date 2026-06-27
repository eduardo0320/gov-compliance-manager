using backend.Data;
using backend.Models;
using backend.Services.Implementations;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class AuditoriaServiceTests
    {
        [Fact]
        public async Task RegistrarEventoAsync_DebePersistirAuditoriaConDatosBasicos()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(dbName)
                .Options;

            await using var context = new NormasDb(options);

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            httpContext.Request.Headers["User-Agent"] = "tests-agent";

            var httpAccessor = new Mock<IHttpContextAccessor>();
            httpAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var logger = new Mock<ILogger<AuditoriaService>>();

            var service = new AuditoriaService(context, httpAccessor.Object, logger.Object);

            await service.RegistrarEventoAsync(
                "LOGIN",
                "Usuario inició sesión",
                "Autenticacion",
                123,
                new { antes = "A" },
                new { despues = "B" });

            var guardado = await context.Auditorias.FirstOrDefaultAsync();
            guardado.Should().NotBeNull();
            guardado!.TipoEvento.Should().Be("LOGIN");
            guardado.Descripcion.Should().Be("Usuario inició sesión");
            guardado.Modulo.Should().Be("Autenticacion");
            guardado.IdUsuario.Should().Be(123);
            guardado.DireccionIp.Should().Be("127.0.0.1");
            guardado.Navegador.Should().Be("tests-agent");
            guardado.DatosAnteriores.Should().NotBeNull();
            guardado.DatosNuevos.Should().NotBeNull();
        }

        [Fact]
        public async Task RegistrarEventoAsync_CuandoNoHayHttpContext_DebePersistirSinIpNiUserAgent()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(dbName)
                .Options;

            await using var context = new NormasDb(options);

            var httpAccessor = new Mock<IHttpContextAccessor>();
            httpAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            var logger = new Mock<ILogger<AuditoriaService>>();
            var service = new AuditoriaService(context, httpAccessor.Object, logger.Object);

            await service.RegistrarEventoAsync("SISTEMA", "Evento sin contexto", "Sistema");

            var guardado = await context.Auditorias.FirstOrDefaultAsync();
            guardado.Should().NotBeNull();
            guardado!.DireccionIp.Should().BeNull();
            guardado.Navegador.Should().BeNull();
        }
    }
}
