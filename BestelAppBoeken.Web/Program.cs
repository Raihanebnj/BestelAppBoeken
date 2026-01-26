// BestelAppBoeken.Web/Program.cs
using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using BestelAppBoeken.Infrastructure.Services;
using BestelAppBoeken.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

// Start application

// ============================================
// HULPFUNCTIE
// ============================================
static bool IsBase64String(string value)
{
    if (string.IsNullOrEmpty(value) || value.Length % 4 != 0)
        return false;

    try
    {
        Convert.FromBase64String(value);
        return true;
    }
    catch
    {
        return false;
    }
}

// ============================================
// ORIGINELE PROGRAM.CS
// ============================================
var builder = WebApplication.CreateBuilder(args);

// Allow overriding RabbitMQ settings with environment variables (recommended for secrets)
// Support common env names: RABBITMQ_HOSTNAME / RABBITMQ_USERNAME / RABBITMQ_PASSWORD
var envRabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME") ?? Environment.GetEnvironmentVariable("RabbitMq__HostName");
var envRabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? Environment.GetEnvironmentVariable("RabbitMq__UserName");
var envRabbitPass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? Environment.GetEnvironmentVariable("RabbitMq__Password");
if (!string.IsNullOrWhiteSpace(envRabbitHost)) builder.Configuration["RabbitMq:HostName"] = envRabbitHost;
if (!string.IsNullOrWhiteSpace(envRabbitUser)) builder.Configuration["RabbitMq:UserName"] = envRabbitUser;
if (!string.IsNullOrWhiteSpace(envRabbitPass)) builder.Configuration["RabbitMq:Password"] = envRabbitPass;

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// CORS configuratie
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrWhiteSpace(origin))
                        return false;

                    var uri = new Uri(origin);
                    return uri.Host == "localhost" ||
                           uri.Host == "127.0.0.1" ||
                           uri.Host == "[::1]";
                })
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(
                    "https://yourdomain.com",
                    "https://www.yourdomain.com"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    });
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuratie - SQLite
builder.Services.AddDbContext<BookstoreDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Custom Services
builder.Services.AddSingleton<IMessageQueueService, RabbitMqService>();
// Register SalesforceService so ISalesforceService can be injected
builder.Services.AddScoped<ISalesforceService, SalesforceService>();

// SAP iDoc Service
builder.Services.AddHttpClient<ISapService, SapService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "Bookstore-SAP-iDoc/1.0");
});

builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IKlantService, KlantService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Database Backup Service
builder.Services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
// Only register the background consumer if RabbitMQ settings are present.
// This prevents startup errors when RabbitMQ is not configured or credentials are invalid.
var rabbitHost = builder.Configuration["RabbitMq:HostName"];
var rabbitUser = builder.Configuration["RabbitMq:UserName"];
if (!string.IsNullOrWhiteSpace(rabbitHost) && !string.IsNullOrWhiteSpace(rabbitUser))
{
    builder.Services.AddHostedService<BestelAppBoeken.Web.Services.OrderUpdateConsumer>();
}

// SSE notifications service
builder.Services.AddSingleton<BestelAppBoeken.Web.Services.OrderNotificationService>();

// PDF Export Service
builder.Services.AddScoped<PdfExportService>();

var app = builder.Build();

// Database initialisatie en seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<BookstoreDbContext>();
        DbSeeder.SeedData(context);

        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fout bij seeden database");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BestelAppBoeken API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors();
app.UseMiddleware<BestelAppBoeken.Web.Middleware.ApiKeyMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "http://localhost:7174";
startupLogger.LogInformation("🚀 Server is gestart op: {Urls}", urls);
startupLogger.LogInformation("📚 Dashboard: http://localhost:7174");
startupLogger.LogInformation("📖 Swagger API: http://localhost:7174/swagger");

app.Run();