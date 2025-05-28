using BooksAPIReviews.Interfaces;
using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Mostrar todas las variables de entorno para diagn�stico
Console.WriteLine("=== VARIABLES DE ENTORNO ===");
foreach (var envVar in Environment.GetEnvironmentVariables().Keys)
{
    Console.WriteLine($"{envVar} = {Environment.GetEnvironmentVariable(envVar.ToString())}");
}

// 2. Obtener la cadena de conexi�n de diferentes fuentes
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                      Environment.GetEnvironmentVariable("DATABASE_URL") ??
                      "Server=nozomi.proxy.rlwy.net;Port=57705;Database=railway;User Id=postgres;Password=RwlvkenbtwHObjzUAZjPywkmLIiYXZut;Ssl Mode=Require;Trust Server Certificate=true;";

Console.WriteLine($"\n=== CADENA DE CONEXI�N UTILIZADA ===");
Console.WriteLine(connectionString);

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("\n=== ERROR: No se pudo obtener la cadena de conexi�n ===");
    Console.WriteLine("Por favor, configura la variable de entorno DATABASE_URL o ConnectionStrings__DefaultConnection");
    Console.WriteLine("Ejemplo para Railway: DATABASE_URL=postgresql://usuario:contrase�a@host:puerto/nombre_bd");
    Environment.Exit(1);
}

// 3. Registrar la conexi�n
builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));

// 4. Configurar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "BooksAPIReviews", Version = "v1" }));

// 5. Registrar servicios personalizados
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<BookDao>();

var app = builder.Build();

// 6. Configurar el pipeline de la aplicaci�n
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Railway")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");