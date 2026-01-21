// BestelAppBoeken.Web/Program.cs
using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using BestelAppBoeken.Infrastructure.Services;
using BestelAppBoeken.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

// ============================================
// ENCRYPTIE TEST VOOR DEMO
// ============================================
Console.WriteLine("🔐 ENCRYPTIE DEMO VOOR SCHOOLPROJECT");
Console.WriteLine("====================================\n");

// Test 1: Basis encryptie/decryptie
var testPassword = "groep3"; // kleine letters!
Console.WriteLine("🧪 TEST 1: Basis encryptie");
Console.WriteLine($"Origineel wachtwoord: '{testPassword}'");

var encrypted = SimpleCrypto.Encrypt(testPassword);
Console.WriteLine($"Geëncrypteerd: '{encrypted}'");
Console.WriteLine($"Lengte: {encrypted.Length} characters");

var decrypted = SimpleCrypto.Decrypt(encrypted);
Console.WriteLine($"Gedecrypteerd: '{decrypted}'");
Console.WriteLine($"Werkt: {testPassword == decrypted}");

// Test 2: Check of het echt encrypted is
Console.WriteLine("\n🧪 TEST 2: Validatie");
bool isEncrypted = encrypted != testPassword &&
                   encrypted.Length > 20 &&
                   !encrypted.Contains(" ");
Console.WriteLine($"Is echt encrypted: {isEncrypted}");
Console.WriteLine($"Is Base64: {IsBase64String(encrypted)}");

// Test 3: Voor appsettings.json
Console.WriteLine("\n📋 VOOR APPSETTINGS.JSON:");
Console.WriteLine("------------------------");
Console.WriteLine("RabbitMQ configuratie:");
Console.WriteLine($"\"Password\": \"{encrypted}\"");
Console.WriteLine("\nSalesforce configuratie:");
Console.WriteLine($"\"Password\": \"{SimpleCrypto.Encrypt("Groep3Rabbit")}\"");

// Test 4: Simuleer RabbitMQ gebruik
Console.WriteLine("\n🧪 TEST 4: RabbitMQ simulatie");
Console.WriteLine("Config lezen → Decrypt → Verbinden");
Console.WriteLine($"Decrypt voor RabbitMQ: '{SimpleCrypto.Decrypt(encrypted)}'");

Console.WriteLine("\n🚀 Start applicatie...\n");

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
// builder.Services.AddScoped<ISalesforceService, SalesforceService>(); // Tijdelijk commenten

// SAP iDoc Service (tijdelijk commenten)
// builder.Services.AddHttpClient<ISapService, SapService>(client =>
// {
//     client.Timeout = TimeSpan.FromSeconds(30);
//     client.DefaultRequestHeaders.Add("User-Agent", "Bookstore-SAP-iDoc/1.0");
// });

builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IKlantService, KlantService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Database Backup Service
builder.Services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
builder.Services.AddHostedService<BestelAppBoeken.Web.Services.OrderUpdateConsumer>();

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