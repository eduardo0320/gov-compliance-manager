using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Integracion
{
    public class RolesFunctionalTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly string _tokenAdmin;
        private readonly string _tokenUsuarioNormal;

        public RolesFunctionalTests(CustomWebApplicationFactory factory)
        {
            _client             = factory.CreateClient();
            _tokenAdmin         = CustomWebApplicationFactory.GenerarToken("ADMIN");
            _tokenUsuarioNormal = CustomWebApplicationFactory.GenerarToken("USUARIO");
            factory.SeedDatabase();
        }

        [Fact]
        public async Task GET_roles_SinToken_DebeRetornar401()
        {
            var response = await _client.GetAsync("/api/roles");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GET_roles_ConTokenAdmin_DebeRetornar200()
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenAdmin);

            var response = await _client.GetAsync("/api/roles");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GET_roles_ConTokenAdmin_DebeRetornarListaDeRoles()
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenAdmin);

            var response = await _client.GetAsync("/api/roles");
            var json     = await response.Content.ReadAsStringAsync();
            var lista    = JsonSerializer.Deserialize<List<JsonElement>>(json);

            lista.Should().NotBeNull();
            lista.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task GET_roles_ConRolUsuarioNormal_DebeRetornar403()
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenUsuarioNormal);

            var response = await _client.GetAsync("/api/roles");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}