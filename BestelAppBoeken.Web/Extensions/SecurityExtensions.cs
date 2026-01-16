using BestelAppBoeken.Web.Middleware;
using BestelAppBoeken.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BestelAppBoeken.Web.Extensions
{
    public static class SecurityExtensions
    {
        public static IServiceCollection AddSecurityServices(this IServiceCollection services)
        {
            // Add GDPR services
            services.AddScoped<GdprCompliantLogger>();
            services.AddScoped<JsonSchemaValidator>();

            // Add HttpClient for security checks
            services.AddHttpClient("SecurityClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }

        public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder app)
        {
            // Order is important!
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
            app.UseMiddleware<RateLimitingMiddleware>();

            return app;
        }
    }
}