using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Iniciando aplicación...");

            var anfitrion = CrearHostBuilder(args).Build();

            // 1. Aplicar migraciones automáticamente al iniciar
            using (var alcance = anfitrion.Services.CreateScope())
            {
                var servicios = alcance.ServiceProvider;
                var db = servicios.GetRequiredService<NormasDb>();

                try
                {
                    Console.WriteLine("🔄 Aplicando migraciones pendientes...");
                    if (db.Database.IsRelational())
                    {
                        await db.Database.MigrateAsync();
                    }
                    else
                    {
                        await db.Database.EnsureCreatedAsync();
                    }       
                    Console.WriteLine("✅ Migraciones aplicadas exitosamente");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al aplicar migraciones: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    throw; // Detener la aplicación si falla la migración
                }
            }

            // 2. Verificar Roles 
            using (var alcance = anfitrion.Services.CreateScope())
            {
                var servicios = alcance.ServiceProvider;
                var rolService = servicios.GetRequiredService<IRolService>();

                try
                {
                    // Verificar si existe cada uno de los roles basicos
                    await rolService.validarRolesExistentes();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al crear roles: {ex.Message}");
                    throw;
                }
            }

            // 3. Usuario admin 
            using (var alcance = anfitrion.Services.CreateScope())
            {
                var servicios = alcance.ServiceProvider;
                var usuarioService = servicios.GetRequiredService<IUsuarioService>();

                try
                {
                    // Verificar si ya existe un admin activo
                    await usuarioService.VerificarUsuarioPorDefecto();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al crear usuario admin: {ex.Message}");
                    throw;
                }
            }

            using (var alcance = anfitrion.Services.CreateScope())
            {
                var servicios = alcance.ServiceProvider;
                var db = servicios.GetRequiredService<NormasDb>();
                var seeder = new DbSeeder(db); // ✅ Crear instancia del seeder

                try
                {
                    Console.WriteLine("🌱 Ejecutando seed de datos...");
                    await seeder.SeedAllAsync(); // ✅ Ejecutar todos los seeds
                    Console.WriteLine("✅ Seed de datos completado");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error en seed de datos: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }

            Console.WriteLine("🎯 Iniciando servidor web...");
            await anfitrion.RunAsync();
            Console.WriteLine("👋 Aplicación detenida");
        }

        public static IHostBuilder CrearHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(constructorWeb =>
                {
                    constructorWeb.UseStartup<Startup>();
                });
    }
}
