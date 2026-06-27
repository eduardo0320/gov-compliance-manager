using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Integracion
{
    public class DominiosFunctionalTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly string _tokenAdmin;

        public DominiosFunctionalTests(CustomWebApplicationFactory factory)
        {
            _client     = factory.CreateClient();
            _tokenAdmin = CustomWebApplicationFactory.GenerarToken("ADMIN");
            factory.SeedDatabase();
        }

        [Fact]
        public async Task GET_dominios_SinAuth_DebeRetornar200()
        {
            var response = await _client.GetAsync("/api/dominios");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GET_dominios_DebeRetornarJson()
        {
            var response = await _client.GetAsync("/api/dominios");
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task GET_dominios_DebeRetornarListaConDatos()
        {
            var response = await _client.GetAsync("/api/dominios");
            var json     = await response.Content.ReadAsStringAsync();
            var lista    = JsonSerializer.Deserialize<List<JsonElement>>(json);

            lista.Should().NotBeNull();
            lista.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task GET_dominios_id_CuandoExiste_DebeRetornar200()
        {
            var response = await _client.GetAsync("/api/dominios/1");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GET_dominios_id_CuandoNoExiste_DebeRetornar404()
        {
            var response = await _client.GetAsync("/api/dominios/9999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GET_dominios_id_procesos_DebeRetornar200()
        {
            var response = await _client.GetAsync("/api/dominios/1/procesos");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GET_dominios_id_procesos_DebeRetornarListaConProcesos()
        {
            var response = await _client.GetAsync("/api/dominios/1/procesos");
            var json     = await response.Content.ReadAsStringAsync();
            var lista    = JsonSerializer.Deserialize<List<JsonElement>>(json);

            lista.Should().NotBeNull();
            lista.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task GET_dominios_tree_DebeRetornar200()
        {
            var response = await _client.GetAsync("/api/dominios/tree");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GET_dominios_arbol_DebeRetornar200()
        {
            var response = await _client.GetAsync("/api/dominios/arbol");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}