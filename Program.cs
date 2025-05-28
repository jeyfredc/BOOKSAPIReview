using BooksAPIReviews.Interfaces;
using BooksAPIReviews.Models;
using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Obtener la cadena de conexión de las variables de entorno
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    // Si no está en las variables de entorno, usa la de appsettings.json
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// 2. Si la cadena está en formato URL (como la de Supabase), convertirla
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
{
    try
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
                         $"Username={userInfo[0]};Password={userInfo[1]};" +
                         "SslMode=Require;Trust Server Certificate=true";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al procesar la cadena de conexión: {ex}");
    }
}

// 3. Validar que tengamos una cadena de conexión
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("No se ha configurado la cadena de conexión a la base de datos.");
}

// 4. Registrar la conexión como singleton para mantener una única instancia
builder.Services.AddSingleton(new NpgsqlConnection(connectionString));

// 5. Configurar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BooksAPIReviews", Version = "v1" });
});

// Registrar servicios personalizados
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UserDao>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<BookDao>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ReviewDao>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<CategoryDao>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<SearchDao>();

var app = builder.Build();

// Configurar el pipeline de la aplicación
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BooksAPIReviews v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Inicializar la conexión al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
    try
    {
        await db.OpenAsync();
        Console.WriteLine("Conexión a la base de datos establecida correctamente");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al conectar a la base de datos: {ex.Message}");
        throw;
    }
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");