using BooksAPIReviews.Interfaces;
using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Obtener la cadena de conexi�n
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

// 2. Si no est� en las variables de entorno, usa la de appsettings.json
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// 3. Validar que tengamos una cadena de conexi�n
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("No se ha configurado la cadena de conexi�n a la base de datos.");
}

// 4. Registrar la conexi�n
builder.Services.AddScoped<NpgsqlConnection>(_ =>
{
    try
    {
        Console.WriteLine($"Cadena de conexi�n: {connectionString}");
        var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al crear la conexi�n: {ex.Message}");
        throw;
    }
});

// 5. Configurar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "BooksAPIReviews", Version = "v1" }));

// Registrar servicios personalizados
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<BookDao>();
// ... (tus otros servicios)

var app = builder.Build();

// Configurar el pipeline de la aplicaci�n
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");