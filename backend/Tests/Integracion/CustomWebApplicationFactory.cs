using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;
using backend.Models;

namespace backend.Tests.Integracion
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private static bool _seeded = false;
        private static readonly object _lock = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseEnvironment("Development");
        }

        public void SeedDatabase()
        {
            lock (_lock)
            {
                if (_seeded) return;

                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NormasDb>();
                db.Database.EnsureCreated();

                if (!db.Roles.Any())
                {
                    db.Roles.Add(new Rol { idRol = 1, nombre = "ADMIN" });
                    db.Usuarios.Add(new Usuario
                    {
                        Id_Usuario         = 1,
                        cedula             = "111111111",
                        nombre             = "Admin Test",
                        correo_electronico = "admin@test.com",
                        contrasena         = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        idRol              = 1
                    });
                    db.Dominios.Add(new Dominio { IdDominio = 1, Nombre = "EDM" });
                    db.Procesos.Add(new Proceso
                    {
                        IdProceso            = 1,
                        Codigo               = "EDM01",
                        Nombre               = "Proceso Test",
                        MarcoNormativo       = "COBIT 2019",
                        EstadoImplementacion = "Activo",
                        DominioId            = 1,
                        CreadoPorId          = 1,
                        ModificadoPorId      = 1
                    });
                    db.Subdominios.Add(new Subdominio
                    {
                        IdSubdominio         = 1,
                        PracticasGobierno    = "Practica Test",
                        IndicadoresAsociados = "Indicador Test",
                        ProcesoId            = 1
                    });
                    db.Actividades.Add(new Actividad
                    {
                        IdActividad                = 1,
                        Nombre                     = "Actividad Test",
                        Implementable              = "Si",
                        SubdominioId               = 1,
                        FuncionariosResponsablesId = 1
                    });
                    db.SaveChanges();
                }

                _seeded = true;
            }
        }

        public static string GenerarToken(string rol = "ADMIN", int usuarioId = 1)
        {
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    "SuperSecretKey12345_MiCITT_Sistema_Normas_2024"));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new System.Security.Claims.Claim("rol", rol)
            };
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer:            "backend",
                audience:          "frontend",
                claims:            claims,
                expires:           DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}