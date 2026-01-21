using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BestelAppBoeken.Web.Middleware
{
    /// <summary>
    /// API-key Authenticatie Middleware (GDPR compliant)
    /// Valideert X-API-Key header voor alle /api/* endpoints
    /// </summary>
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private const string API_KEY_HEADER = "X-API-Key";

        public ApiKeyMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check of dit een API endpoint is
            var path = context.Request.Path.ToString();
            
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                // In DEVELOPMENT mode: Alle API calls zijn toegestaan zonder API key
                var isDevelopment = context.RequestServices
                    .GetRequiredService<IWebHostEnvironment>()
                    .IsDevelopment();

                if (isDevelopment)
                {
                    _logger.LogInformation("🔓 [API Auth] Development mode - API-key NIET vereist voor {Path}", path);
                    await _next(context);
                    return;
                }

                // Uitzonderingen: Publieke GET endpoints
                var publicEndpoints = new[]
                {
                    "/api/books",     // Boeken catalogus -
                    "/api/klanten",   // Klanten lijst voor dropdown
                    "/api/orders"     // Orders lijst
                };

                bool isPublicGetRequest = context.Request.Method == "GET" && 
                    publicEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));

                if (isPublicGetRequest)
                {
                    _logger.LogInformation("🔓 [API Auth] Public GET endpoint - API-key NIET vereist voor {Path}", path);
                    await _next(context);
                    return;
                }

                // Check of API-Key header aanwezig is
                if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
                {
                    _logger.LogWarning("❌ [API Auth] Missing API-key for {Path}", path);
                    
                    context.Response.StatusCode = 401; // Unauthorized
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "API-key vereist",
                        message = "Voeg X-API-Key header toe aan je request",
                        documentation = "/swagger"
                    });
                    return;
                }

                // Haal geldige API-keys op uit configuratie
                var validApiKey = _configuration["ApiKey:ValidKey"];
                
                if (string.IsNullOrWhiteSpace(validApiKey))
                {
                    _logger.LogError("❌ [API Auth] CONFIGURATIE FOUT - Geen API-key geconfigureerd in appsettings.json");
                    
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Server configuratie fout",
                        message = "API-key niet geconfigureerd"
                    });
                    return;
                }

                // Valideer API-key
                if (!extractedApiKey.Equals(validApiKey))
                {
                    _logger.LogWarning("❌ [API Auth] Ongeldige API-key gebruikt voor {Path}", path);
                    
                    context.Response.StatusCode = 403; // Forbidden
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Ongeldige API-key",
                        message = "De opgegeven API-key is niet geldig"
                    });
                    return;
                }

                // API-key is geldig
                _logger.LogInformation("✅ [API Auth] Geldige API-key voor {Path}", path);
            }

            // Ga door naar volgende middleware
            await _next(context);
        }
    }
}
