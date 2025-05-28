using BooksAPIReviews.Interfaces;
using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración básica
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 2. Configuración del logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 3. Obtener y validar la cadena de conexión
string connectionString = GetConnectionString(builder.Configuration, builder.Environment);

// 4. Validar que tengamos una cadena de conexión
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("=== ERROR: No se pudo obtener la cadena de conexión ===");
    Console.WriteLine("Variables de entorno disponibles:");
    foreach (var envVar in Environment.GetEnvironmentVariables().Keys)
    {
        Console.WriteLine($"{envVar} = {Environment.GetEnvironmentVariable(envVar.ToString())}");
    }
    throw new InvalidOperationException("No se pudo obtener la cadena de conexión de ninguna fuente.");
}

// 5. Registrar servicios
ConfigureServices(builder.Services, connectionString);

var app = builder.Build();

// 6. Configurar el pipeline HTTP
ConfigurePipeline(app);

app.Run();

// Métodos auxiliares
static string GetConnectionString(IConfiguration configuration, IHostEnvironment env)
{
    try
    {
        // 1. Intentar obtener de la variable de entorno de Railway
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        // 2. Si estamos en Railway y tenemos DATABASE_URL, formatearla
        if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Usando DATABASE_URL de Railway");
            return ConvertPostgresUrlToConnectionString(databaseUrl);
        }

        // 3. Intentar obtener de la configuración
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Usando cadena de conexión de configuración");
            return connectionString;
        }

        // 4. Valor por defecto para desarrollo local
        if (env.IsDevelopment())
        {
            Console.WriteLine("Usando cadena de conexión de desarrollo local");
            return "Host=localhost;Database=booksdb;Username=postgres;Password=tu_contraseña;";
        }

        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al obtener la cadena de conexión: {ex.Message}");
        throw;
    }
}

static string ConvertPostgresUrlToConnectionString(string url)
{
    try
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
            SslMode = SslMode.Require,
            TrustServerCertificate = true,
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 20
        };

        return builder.ConnectionString;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al convertir la URL de la base de datos: {ex.Message}");
        throw new FormatException("Formato de URL de base de datos no válido", ex);
    }
}

static void ConfigureServices(IServiceCollection services, string connectionString)
{
    // 1. Registrar la conexión como singleton (opcional, puedes usar scoped si lo prefieres)
    services.AddSingleton(_ =>
    {
        var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    });

    // 2. Configurar servicios de la aplicación
    services.AddControllers();

    // 3. Configurar Swagger
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "BooksAPIReviews",
            Version = "v1",
            Description = "API de reseñas de libros"
        });
    });

    // 4. Registrar servicios personalizados
    services.AddScoped<IBookService, BookService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ICategoryService, CategoryService>();
    services.AddScoped<IReviewService, ReviewService>();
    services.AddScoped<ISearchService, SearchService>();
    services.AddScoped<IUserService, UserService>();

    // 5. Registrar DAOs
    services.AddScoped<BookDao>();
    services.AddScoped<CategoryDao>();
    services.AddScoped<ReviewDao>();
    services.AddScoped<SearchDao>();
    services.AddScoped<UserDao>();
}

static void ConfigurePipeline(WebApplication app)
{
    // 1. Configurar el entorno de desarrollo
    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "BooksAPIReviews v1");
            c.RoutePrefix = "swagger";
        });
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    // 2. Configuración común para todos los entornos
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();

    // 3. Configurar CORS
    app.UseCors(policy =>
        policy.WithOrigins(
                "http://localhost:3000",
                "https://your-production-domain.com")
              .AllowAnyMethod()
              .AllowAnyHeader());

    // 4. Configurar endpoints
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapGet("/health", () => "Healthy");
    });

    // 5. Redirección a Swagger en desarrollo
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }
}