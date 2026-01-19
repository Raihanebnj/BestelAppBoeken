using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using BestelAppBoeken.Infrastructure.Services;
using BestelAppBoeken.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Zorg ervoor dat JSON properties in camelCase worden geserialiseerd (bijv. "naam" in plaats van "Naam")
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Swagger/OpenAPI configuratie
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuratie - SQLite
builder.Services.AddDbContext<BookstoreDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Custom Services
builder.Services.AddSingleton<IMessageQueueService, RabbitMqService>();
builder.Services.AddScoped<ISalesforceService, SalesforceService>();
builder.Services.AddScoped<ISapService, SapService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IKlantService, KlantService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// ? Database Backup Service (nieuw toegevoegd)
builder.Services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
builder.Services.AddHostedService<BestelAppBoeken.Web.Services.OrderUpdateConsumer>();

var app = builder.Build();

// Database initialisatie en seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<BookstoreDbContext>();

        // Optie 1: Gebruik DbSeeder (standaard)
        DbSeeder.SeedData(context);

        // Optie 2: Extra hardcoded data direct in Program.cs
        logger.LogInformation("?? Extra hardcoded data wordt toegevoegd...");

        // Check of er al extra data is
        if (!context.Klanten.Any(k => k.Email == "sophie.vermeulen@example.be"))
        {
            // Hardcoded EXTRA klanten (naast de 3 in DbSeeder)
            var extraKlanten = new[]
            {
                new Klant
                {
                    Naam = "Sophie Vermeulen",
                    Email = "sophie.vermeulen@example.be",
                    Telefoon = "0478123456",
                    Adres = "Kerkstraat 45, 1000 Brussel"
                },
                new Klant
                {
                    Naam = "Lucas Dubois",
                    Email = "lucas.dubois@example.be",
                    Telefoon = "0479234567",
                    Adres = "Meir 123, 2000 Antwerpen"
                },
                new Klant
                {
                    Naam = "Emma Van Der Berg",
                    Email = "emma.vanderberg@example.be",
                    Telefoon = "0476345678",
                    Adres = "Korenmarkt 8, 9000 Gent"
                }
            };

            context.Klanten.AddRange(extraKlanten);
            logger.LogInformation($"? {extraKlanten.Length} extra klanten toegevoegd");
        }

        // Hardcoded EXTRA boeken (naast de 50 in DbSeeder)
        if (!context.Books.Any(b => b.Isbn == "PROG-001"))
        {
            var extraBoeken = new[]
            {
                new Book
                {
                    Title = "De Kunst van Programmeren",
                    Author = "Donald Knuth",
                    Price = 89.99m,
                    Isbn = "PROG-001",
                    VoorraadAantal = 25,
                    Description = "Klassiek werk over algoritmen en datastructuren"
                },
                new Book
                {
                    Title = "Clean Code: A Handbook of Agile Software Craftsmanship",
                    Author = "Robert C. Martin",
                    Price = 54.99m,
                    Isbn = "PROG-002",
                    VoorraadAantal = 40,
                    Description = "Gids voor het schrijven van leesbare en onderhoudbare code"
                },
                new Book
                {
                    Title = "The Pragmatic Programmer",
                    Author = "Andrew Hunt & David Thomas",
                    Price = 52.50m,
                    Isbn = "PROG-003",
                    VoorraadAantal = 35,
                    Description = "Essenties van praktisch softwareontwikkeling"
                }
            };

            context.Books.AddRange(extraBoeken);
            logger.LogInformation($"? {extraBoeken.Length} extra boeken toegevoegd");
        }

        context.SaveChanges();
        logger.LogInformation("?? Extra hardcoded data succesvol opgeslagen!");

        logger.LogInformation("? Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "? Er is een fout opgetreden bij het seeden van de database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Enable Swagger (beschikbaar op /swagger)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BestelAppBoeken API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// BELANGRIJK: UseDefaultFiles moet VOOR UseStaticFiles komen
app.UseDefaultFiles(); // Dit zorgt ervoor dat index.html automatisch wordt geladen bij /
app.UseStaticFiles();  // Serve static files uit wwwroot

app.UseRouting();

app.UseAuthorization();

// MVC routes blijven beschikbaar voor API endpoints of andere functionaliteit
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();