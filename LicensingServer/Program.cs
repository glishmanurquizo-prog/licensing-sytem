using LicensingServer.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🔌 Configuración de PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 📦 Servicios
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 🌐 🔥 CONFIGURACIÓN CLAVE PARA RAILWAY
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");

// 📌 Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ⚠️ OPCIONAL (puedes dejarlo o quitarlo)
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();