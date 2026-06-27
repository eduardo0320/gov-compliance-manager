using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Integracion
{
    public class TransparenciaFunctionalTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public TransparenciaFunctionalTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        // ──────────────────────────────────────────────
        //  GET /api/transparencia/documentos
        //  Endpoint público — no requiere autenticación
        // ──────────────────────────────────────────────

        [Fact]
        public async Task GET_transparencia_documentos_SinAuth_DebeRetornar200()
        {
            var response = await _client.GetAsync("/api/transparencia/documentos");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GET_transparencia_documentos_DebeRetornarJson()
        {
            var response = await _client.GetAsync("/api/transparencia/documentos");

            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task GET_transparencia_documentos_DebeRetornarEstructuraEsperada()
        {
            var response = await _client.GetAsync("/api/transparencia/documentos");
            var json     = await response.Content.ReadAsStringAsync();
            var doc      = JsonSerializer.Deserialize<JsonElement>(json);

            // Debe tener las propiedades del resumen
            doc.TryGetProperty("totalDocumentos", out _).Should().BeTrue();
            doc.TryGetProperty("totalDominios",   out _).Should().BeTrue();
            doc.TryGetProperty("dominios",         out _).Should().BeTrue();
        }

        // ──────────────────────────────────────────────
        //  GET /api/transparencia/documentos/{id}/descargar
        // ──────────────────────────────────────────────

        [Fact]
        public async Task GET_transparencia_descargar_CuandoNoExiste_DebeRetornar404()
        {
            var response = await _client.GetAsync("/api/transparencia/documentos/9999/descargar");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GET_transparencia_descargar_CuandoNoExiste_DebeRetornarMensajeError()
        {
            var response = await _client.GetAsync("/api/transparencia/documentos/9999/descargar");
            var json     = await response.Content.ReadAsStringAsync();
            var doc      = JsonSerializer.Deserialize<JsonElement>(json);

            doc.TryGetProperty("error", out _).Should().BeTrue();
        }
    }
}
