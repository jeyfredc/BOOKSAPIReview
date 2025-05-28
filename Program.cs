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

// 1. Configuraci�n b�sica
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 2. Configuraci�n del logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 3. Obtener la cadena de conexi�n
var connectionString = GetConnectionString(builder.Configuration, builder.Environment);
Console.WriteLine($"Cadena de conexi�n: {ObfuscateConnectionString(connectionString)}");

// 4. Validar que tengamos una cadena de conexi�n
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("=== ERROR: No se pudo obtener la cadena de conexi�n ===");
    Console.WriteLine("Variables de entorno disponibles:");
    foreach (var envVar in Environment.GetEnvironmentVariables().Keys)
    {
        Console.WriteLine($"{envVar} = {Environment.GetEnvironmentVariable(envVar.ToString())}");
    }
    throw new InvalidOperationException("No se pudo obtener la cadena de conexi�n de ninguna fuente.");
}

// 5. Configurar servicios
ConfigureServices(builder.Services, connectionString);

var app = builder.Build();

// 6. Configurar el pipeline HTTP
ConfigurePipeline(app);

// 7. Iniciar la aplicaci�n
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
Console.WriteLine($"Iniciando aplicaci�n en el puerto {port}");
app.Run($"http://0.0.0.0:{port}");

// M�todos auxiliares
static string GetConnectionString(IConfiguration configuration, IHostEnvironment env)
{
    try
    {
        // 1. Intenta obtener directamente de la configuraci�n
        var connectionString = configuration.GetValue<string>("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Usando cadena de conexi�n de DefaultConnection");
            return connectionString;
        }

        // 2. Intenta obtener de ConnectionStrings:DefaultConnection
        connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Usando cadena de conexi�n de ConnectionStrings:DefaultConnection");
            return connectionString;
        }

        // 3. Intenta obtener de la variable de entorno DATABASE_URL (formato Railway)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(databaseUrl))
        {
            Console.WriteLine("Usando DATABASE_URL de Railway");
            return ConvertPostgresUrlToConnectionString(databaseUrl);
        }

        // 4. Valor por defecto para desarrollo local
        if (env.IsDevelopment())
        {
            Console.WriteLine("Usando cadena de conexi�n de desarrollo local");
            return "Host=localhost;Database=booksdb;Username=postgres;Password=tu_contrase�a;";
        }

        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al obtener la cadena de conexi�n: {ex.Message}");
        return null;
    }
}

static string ConvertPostgresUrlToConnectionString(string databaseUrl)
{
    try
    {
        // Si ya es una cadena de conexi�n completa, devu�lvela tal cual
        if (databaseUrl.Contains("Host=") || databaseUrl.Contains("Server="))
        {
            return databaseUrl;
        }

        // Si es una URL de Railway (postgres://...)
        if (databaseUrl.StartsWith("postgres://"))
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');

            return new NpgsqlConnectionStringBuilder
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
            }.ToString();
        }

        return databaseUrl;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al convertir la URL de la base de datos: {ex.Message}");
        return databaseUrl; // Devuelve la cadena original si hay un error
    }
}

static string ObfuscateConnectionString(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return "[Cadena de conexi�n vac�a]";

    try
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "***";
        }
        return builder.ToString();
    }
    catch
    {
        return "[Cadena de conexi�n no v�lida]";
    }
}

static void ConfigureServices(IServiceCollection services, string connectionString)
{
    // Registrar la conexi�n como Singleton
    services.AddSingleton(_ =>
    {
        var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    });

    // Configurar servicios de la aplicaci�n
    services.AddControllers();

    // Configurar Swagger
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "BooksAPIReviews",
            Version = "v1",
            Description = "API de rese�as de libros"
        });
    });

    // Registrar servicios personalizados
    services.AddScoped<IBookService, BookService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ICategoryService, CategoryService>();
    services.AddScoped<IReviewService, ReviewService>();
    services.AddScoped<ISearchService, SearchService>();
    services.AddScoped<IUserService, UserService>();

    // Registrar DAOs
    services.AddScoped<BookDao>();
    services.AddScoped<CategoryDao>();
    services.AddScoped<ReviewDao>();
    services.AddScoped<SearchDao>();
    services.AddScoped<UserDao>();
}

static void ConfigurePipeline(WebApplication app)
{
    // Configurar el entorno de desarrollo
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

    // Configuraci�n com�n para todos los entornos
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();

    // Configurar CORS
    app.UseCors(policy =>
        policy.WithOrigins(
                "http://localhost:3000",
                "https://booksapireview-production.up.railway.app")
              .AllowAnyMethod()
              .AllowAnyHeader());

    // Configurar endpoints
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapGet("/health", () => "Healthy");
    });

    // Redirecci�n a Swagger en desarrollo
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }
}