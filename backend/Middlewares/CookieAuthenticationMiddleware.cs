using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace backend.Middleware
{
    public class CookieAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public CookieAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Leer el token de la cookie
            var token = context.Request.Cookies["token"];

            // Si existe la cookie, agregarla al header Authorization
            if (!string.IsNullOrEmpty(token))
            {
                context.Request.Headers["Authorization"] = $"Bearer {token}";
            }

            // Continuar con el siguiente middleware
            await _next(context);
        }
    }

    // Extensión para registrar el middleware fácilmente
    public static class CookieAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseCookieAuthentication(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CookieAuthenticationMiddleware>();
        }
    }
}
