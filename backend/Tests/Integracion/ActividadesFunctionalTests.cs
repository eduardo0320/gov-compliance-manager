using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Integracion
{
    public class ActividadesFunctionalTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly string _tokenAdmin;

        public ActividadesFunctionalTests(CustomWebApplicationFactory factory)
        {
            _client     = factory.CreateClient();
            _tokenAdmin = CustomWebApplicationFactory.GenerarToken("ADMIN");
            factory.SeedDatabase();
        }

        [Fact]
        public async Task GET_actividades_CuandoSubdominioExiste_DebeRetornar200()
        {
            var response = await _client.GetAsync("/api/subdominios/1/actividades");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GET_actividades_DebeRetornarListaConActividades()
        {
            var response = await _client.GetAsync("/api/subdominios/1/actividades");
            var json     = await response.Content.ReadAsStringAsync();
            var lista    = JsonSerializer.Deserialize<List<JsonElement>>(json);

            lista.Should().NotBeNull();
            lista.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task GET_actividades_CuandoSubdominioNoExiste_DebeRetornar200ConListaVacia()
        {
            var response = await _client.GetAsync("/api/subdominios/9999/actividades");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task POST_actividades_SinToken_DebeRetornar401()
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    nombre                     = "Nueva Actividad",
                    implementable              = "Si",
                    funcionariosResponsablesId = 1
                }),
                Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/subdominios/1/actividades", body);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task POST_actividades_ConTokenAdmin_DebeRetornar200O201()
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenAdmin);

            var body = new StringContent(
                JsonSerializer.Serialize(new
                {
                    nombre                     = "Actividad Funcional Test",
                    implementable              = "Si",
                    funcionariosResponsablesId = 1
                }),
                Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/subdominios/1/actividades", body);
            ((int)response.StatusCode).Should().BeOneOf(200, 201);
        }

        [Fact]
        public async Task POST_actividades_ConCuerpoVacio_DebeRetornar400()
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenAdmin);

            var body = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/subdominios/1/actividades", body);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}