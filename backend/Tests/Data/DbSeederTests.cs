using Xunit;
using FluentAssertions;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Data
{
    public class DbSeederTests
    {
        private NormasDb CrearContextoEnMemoria()
        {
            var options = new DbContextOptionsBuilder<NormasDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new NormasDb(options);
        }

        [Fact]
        public async Task SeedDominiosCobitAsync_DebeCrearCincoDominios_YSerIdempotente()
        {
            using var context = CrearContextoEnMemoria();
            var seeder = new DbSeeder(context);

            await seeder.SeedDominiosCobitAsync();
            await seeder.SeedDominiosCobitAsync();

            var dominios = await context.Dominios.ToListAsync();
            dominios.Should().HaveCount(5);
            dominios.Should().Contain(d => d.Nombre.StartsWith("EDM"));
            dominios.Should().Contain(d => d.Nombre.StartsWith("APO"));
            dominios.Should().Contain(d => d.Nombre.StartsWith("BAI"));
            dominios.Should().Contain(d => d.Nombre.StartsWith("DSS"));
            dominios.Should().Contain(d => d.Nombre.StartsWith("MEA"));
        }

        [Fact]
        public async Task SeedProcesosCobitAsync_DebeCrearCuarentaProcesos_YSerIdempotente()
        {
            using var context = CrearContextoEnMemoria();
            var seeder = new DbSeeder(context);

            await seeder.SeedDominiosCobitAsync();
            await seeder.SeedProcesosCobitAsync();
            await seeder.SeedProcesosCobitAsync();

            var procesos = await context.Procesos.ToListAsync();
            procesos.Should().HaveCount(40);
            procesos.Should().Contain(p => p.Codigo == "EDM01");
            procesos.Should().Contain(p => p.Codigo == "APO14");
            procesos.Should().Contain(p => p.Codigo == "MEA04");
        }

        [Fact]
        public async Task SeedSubdominiosCobitAsync_DebeCrearCuarentaSubdominios_YSerIdempotente()
        {
            using var context = CrearContextoEnMemoria();
            var seeder = new DbSeeder(context);

            await seeder.SeedDominiosCobitAsync();
            await seeder.SeedProcesosCobitAsync();
            await seeder.SeedSubdominiosCobitAsync();
            await seeder.SeedSubdominiosCobitAsync();

            var subdominios = await context.Subdominios.ToListAsync();
            subdominios.Should().HaveCount(40);
            subdominios.Should().Contain(s => s.ProcesoId == 1);
            subdominios.Should().Contain(s => s.ProcesoId == 40);
        }

        [Fact]
        public async Task SeedActividadesEjemploAsync_DebeCrearActividades_YSerIdempotente()
        {
            using var context = CrearContextoEnMemoria();
            var seeder = new DbSeeder(context);

            // Usuario base para FK FuncionariosResponsablesId = 1
            context.Roles.Add(new Models.Rol { idRol = 1, nombre = "Admin" });
            context.Usuarios.Add(new Models.Usuario
            {
                Id_Usuario = 1,
                cedula = "000000001",
                nombre = "Seeder User",
                correo_electronico = "seeder@test.com",
                contrasena = "hash",
                idRol = 1
            });
            await context.SaveChangesAsync();

            await seeder.SeedDominiosCobitAsync();
            await seeder.SeedProcesosCobitAsync();
            await seeder.SeedSubdominiosCobitAsync();

            await seeder.SeedActividadesEjemploAsync();
            await seeder.SeedActividadesEjemploAsync();

            var actividades = await context.Actividades.ToListAsync();
            actividades.Should().HaveCount(20);
            actividades.Should().Contain(a => a.Nombre.Contains("gobierno de TI"));
        }

        [Fact]
        public async Task SeedAllAsync_DebeCrearEstructuraCompleta()
        {
            using var context = CrearContextoEnMemoria();
            var seeder = new DbSeeder(context);

            // Usuario base para FK FuncionariosResponsablesId = 1
            context.Roles.Add(new Models.Rol { idRol = 1, nombre = "Admin" });
            context.Usuarios.Add(new Models.Usuario
            {
                Id_Usuario = 1,
                cedula = "000000001",
                nombre = "Seeder User",
                correo_electronico = "seeder@test.com",
                contrasena = "hash",
                idRol = 1
            });
            await context.SaveChangesAsync();

            await seeder.SeedAllAsync();

            (await context.Dominios.CountAsync()).Should().Be(5);
            (await context.Procesos.CountAsync()).Should().Be(40);
            (await context.Subdominios.CountAsync()).Should().Be(40);
            (await context.Actividades.CountAsync()).Should().Be(20);
        }
    }
}
