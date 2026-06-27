using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Services.Interfaces;
using backend.Services.Implementations;


namespace backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configurar Entity Framework — en Testing se usa BD en memoria, en producción MySQL
            var usarBdEnMemoria = Configuration["USE_IN_MEMORY_DB"] == "true";
            if (usarBdEnMemoria)
            {
                // Nombre FIJO para que todos los scopes compartan la misma instancia
                services.AddDbContext<NormasDb>(options =>
                    options.UseInMemoryDatabase("FunctionalTestDb"));
            }
            else
            {
                services.AddDbContext<NormasDb>(options =>
                    options.UseMySql(
                        Configuration.GetConnectionString("DefaultConnection"),
                        new MySqlServerVersion(new Version(8, 0, 36))
                    )
                );
            }

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            var origenesPermitidos = Configuration
                .GetSection("Cors:OrigenesPermitidos")
                .Get<string[]>() ?? ["http://localhost:3000"];

            services.AddCors(options =>
            {
                options.AddPolicy("PermitirCookies", builder =>
                {
                    builder
                        .WithOrigins(origenesPermitidos)
                        .AllowCredentials()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Content-Disposition");
                });
            });

            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("transparencia", opt =>
                {
                    opt.PermitLimit = 60;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"error\": \"Demasiadas solicitudes. Intente nuevamente en un momento.\"}",
                        cancellationToken);
                };
            });

            var jwtSettings = Configuration.GetSection("JWT");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "SuperSecretKey12345");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "backend",
                    ValidAudience = jwtSettings["Audience"] ?? "frontend",
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    RoleClaimType = "rol"
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.ContainsKey("token"))
                        {
                            context.Token = context.Request.Cookies["token"];
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOrSuperadmin", policy =>
                    policy.RequireClaim("rol", "ADMIN", "SUPERADMIN")
                );
            });

            services.AddHttpContextAccessor();
            services.AddHttpClient(); // Para descarga de documentos desde URL
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IEmailService, EmailService>();

            services.AddScoped<backend.Repositories.Interfaces.IUsuarioRepository, backend.Repositories.Implementations.UsuarioRepository>();
            services.AddScoped<backend.Repositories.Interfaces.IRolRepository, backend.Repositories.Implementations.RolRepository>();
            services.AddScoped<backend.Repositories.Interfaces.IDominioRepository, backend.Repositories.Implementations.DominioRepository>();
            services.AddScoped<backend.Repositories.Interfaces.IProcesoRepository, backend.Repositories.Implementations.ProcesoRepository>();
            services.AddScoped<backend.Repositories.Interfaces.ISubdominioRepository, backend.Repositories.Implementations.SubdominioRepository>();
            services.AddScoped<backend.Repositories.Interfaces.IActividadRepository, backend.Repositories.Implementations.ActividadRepository>();
            services.AddScoped<backend.Repositories.Interfaces.IAuditoriaRepository, backend.Repositories.Implementations.AuditoriaRepository>();

            services.AddScoped<backend.Repositories.Interfaces.IDocumentoRepository, backend.Repositories.Implementations.DocumentoRepository>();
            services.AddScoped<backend.Repositories.Interfaces.IVersionDocumentoRepository, backend.Repositories.Implementations.VersionDocumentoRepository>();
            services.AddScoped<backend.Repositories.Interfaces.IRelacionDocumentoRepository, backend.Repositories.Implementations.RelacionDocumentoRepository>();
            services.AddScoped<backend.Repositories.Interfaces.INotificacionRepository, backend.Repositories.Implementations.NotificacionRepository>();
            services.AddScoped<backend.Repositories.Interfaces.IDashboardRepository, backend.Repositories.Implementations.DashboardRepository>();

            services.AddScoped<backend.Services.Interfaces.IUsuarioService, backend.Services.Implementations.UsuarioService>();
            services.AddScoped<backend.Services.Interfaces.IRolService, backend.Services.Implementations.RolService>();
            services.AddScoped<backend.Services.Interfaces.IDominioService, backend.Services.Implementations.DominioService>();
            services.AddScoped<backend.Services.Interfaces.IProcesoService, backend.Services.Implementations.ProcesoService>();
            services.AddScoped<backend.Services.Interfaces.ISubdominioService, backend.Services.Implementations.SubdominioService>();
            services.AddScoped<backend.Services.Interfaces.IActividadService, backend.Services.Implementations.ActividadService>();
            services.AddScoped<backend.Services.Interfaces.IAutenticacionService, backend.Services.Implementations.AutenticacionService>();
            services.AddScoped<backend.Services.Interfaces.IEmailService, backend.Services.Implementations.EmailService>();

            services.Configure<backend.Config.DocumentosConfig>(
                Configuration.GetSection("DocumentosConfig"));

            services.AddScoped<backend.Services.Interfaces.IAlmacenamientoService, backend.Services.Implementations.AlmacenamientoService>();
            services.AddScoped<backend.Services.Interfaces.IIntegridadService, backend.Services.Implementations.IntegridadService>();
            services.AddScoped<backend.Services.Interfaces.IHistorialActividadService, backend.Services.Implementations.HistorialActividadService>();
            services.AddScoped<backend.Services.Interfaces.IVersionDocumentoService, backend.Services.Implementations.VersionDocumentoService>();
            services.AddScoped<backend.Services.Interfaces.ITransparenciaService, backend.Services.Implementations.TransparenciaService>();
            services.AddScoped<backend.Services.Interfaces.IDocumentoService, backend.Services.Implementations.DocumentoService>();
            services.AddScoped<backend.Services.Interfaces.INotificacionService, backend.Services.Implementations.NotificacionService>();
            services.AddScoped<backend.Services.Interfaces.IDashboardService, backend.Services.Implementations.DashboardService>();

            services.AddHostedService<backend.Services.Implementations.LimpiezaTemporalService>();
            services.AddHostedService<backend.Services.Implementations.AlertasVencimientoService>();
            services.AddHostedService<backend.Services.Implementations.BackupService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("PermitirCookies");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapGet("/transparencia", async context =>
                {
                    var env2 = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                    var file = Path.Combine(env2.WebRootPath, "transparencia", "index.html");
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.SendFileAsync(file);
                });
            });
        }
    }
}