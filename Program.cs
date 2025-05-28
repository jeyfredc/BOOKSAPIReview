using BooksAPIReviews.Interfaces;
using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar configuración
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 2. Obtener la cadena de conexión de diferentes fuentes
var connectionString =
    // 1. Intenta obtener de la variable de entorno específica
    Environment.GetEnvironmentVariable("DefaultConnection") ??
    // 2. Intenta obtener de la variable de entorno de Railway
    Environment.GetEnvironmentVariable("DATABASE_URL") ??
    // 3. Intenta obtener de la configuración
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    // 4. Valor por defecto
    "Server=nozomi.proxy.rlwy.net;Port=57705;Database=railway;User Id=postgres;Password=RwlvkenbtwHObjzUAZjPywkmLIiYXZut;Ssl Mode=Require;Trust Server Certificate=true;";

// 3. Si es una URL de Railway, formatearla
if (connectionString.StartsWith("postgres://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString =
        $"Server={uri.Host};" +
        $"Port={uri.Port};" +
        $"Database={uri.AbsolutePath.TrimStart('/')};" +
        $"User Id={userInfo[0]};" +
        $"Password={userInfo[1]};" +
        "Ssl Mode=Require;Trust Server Certificate=true;";
}

// 4. Validar que tengamos una cadena de conexión
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("\n=== ERROR: No se pudo obtener la cadena de conexión ===");
    Console.WriteLine("Variables de entorno disponibles:");
    foreach (var envVar in Environment.GetEnvironmentVariables().Keys)
    {
        Console.WriteLine($"{envVar} = {Environment.GetEnvironmentVariable(envVar.ToString())}");
    }
    throw new InvalidOperationException("No se pudo obtener la cadena de conexión de ninguna fuente.");
}

// 5. Mostrar información de depuración (sin mostrar contraseña)
var safeConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
{
    Password = "***"
}.ToString();
Console.WriteLine($"\n=== CADENA DE CONEXIÓN UTILIZADA ===");
Console.WriteLine(safeConnectionString);

// 6. Registrar la conexión
builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));

// 7. Configurar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "BooksAPIReviews", Version = "v1" }));

// 8. Registrar servicios personalizados
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<BookDao>();
builder.Services.AddScoped<CategoryDao>();
builder.Services.AddScoped<ReviewDao>();
builder.Services.AddScoped<SearchDao>();
builder.Services.AddScoped<UserDao>();



var app = builder.Build();

// 9. Configurar el pipeline de la aplicación
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "production")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");