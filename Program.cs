using BooksAPIReviews.Interfaces;
using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

// 5. Configurar Kestrel
var httpPort = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var httpsPort = Environment.GetEnvironmentVariable("HTTPS_PORT") ?? "443";

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(int.Parse(httpPort));
    serverOptions.ListenAnyIP(int.Parse(httpsPort), listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

// 6. Configurar servicios
ConfigureServices(builder.Services, connectionString);

var app = builder.Build();

// 7. Configurar el pipeline HTTP
ConfigurePipeline(app);

// 8. Iniciar la aplicaci�n
app.Urls.Clear();
app.Urls.Add($"[http://0.0.0.0](http://0.0.0.0):{httpPort}");
app.Urls.Add($"[https://0.0.0.0](https://0.0.0.0):{httpsPort}");

Console.WriteLine($"Iniciando aplicaci�n en:");
Console.WriteLine($"- HTTP: [http://0.0.0.0](http://0.0.0.0):{httpPort}");
Console.WriteLine($"- HTTPS: [https://0.0.0.0](https://0.0.0.0):{httpsPort}");

app.Run();

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
        return databaseUrl;
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
    // Configuraci�n de CORS
    services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    // Configuraci�n de controladores
    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });

    // Configuraci�n de Swagger
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

    // Configuraci�n de la base de datos
    services.AddSingleton(_ =>
    {
        var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
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

    // Configuraci�n de autenticaci�n (si es necesaria)
    // services.AddAuthentication(...)
    // services.AddAuthorization(...);
}

static void ConfigurePipeline(WebApplication app)
{
    // Configuraci�n del entorno de desarrollo
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

    // Configuraci�n de redirecci�n HTTPS
    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    else
    {
        var httpsPort = Environment.GetEnvironmentVariable("HTTPS_PORT") ?? "443";
        app.UseHttpsRedirection(new HttpsRedirectionOptions
        {
            HttpsPort = int.Parse(httpsPort)
        });
    }

    // Configuraci�n de headers de seguridad
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseRouting();

    // Configuraci�n de CORS
    app.UseCors("AllowAll");

    app.UseAuthorization();

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