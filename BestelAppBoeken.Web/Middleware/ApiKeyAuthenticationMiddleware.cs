using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BestelAppBoeken.Web.Middleware
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER = "X-API-Key";
        private const string VALID_API_KEY = "your-secure-api-key-here"; // Verplaats naar config/geheimen

        public ApiKeyAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for health checks
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key ontbreekt");
                return;
            }

            if (!VALID_API_KEY.Equals(extractedApiKey))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Ongeldige API Key");
                return;
            }




            await _next(context);
        }
    }
}
