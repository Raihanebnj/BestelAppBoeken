using BestelAppBoeken.Core.Interfaces;
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

var app = builder.Build();

// Database initialisatie en seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BookstoreDbContext>();
        DbSeeder.SeedData(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
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
