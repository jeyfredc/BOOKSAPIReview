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
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 2. Configuraci�n del logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 3. Obtener configuraci�n de la base de datos
var connectionString = GetConnectionString(builder.Configuration, builder.Environment);

// 4. Registrar servicios
ConfigureServices(builder.Services, connectionString);

var app = builder.Build();

// 5. Configurar el pipeline HTTP
ConfigurePipeline(app);

app.Run();

// M�todos auxiliares
static string GetConnectionString(IConfiguration configuration, IHostEnvironment env)
{
    // 1. Intentar obtener de la variable de entorno espec�fica
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

    // 2. Si no est� en la variable de entorno, obtener del archivo de configuraci�n
    if (string.IsNullOrEmpty(connectionString))
    {
        connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // 3. Si es una URL de Railway (formato postgres://)
    if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
    {
        connectionString = ConvertPostgresUrlToConnectionString(connectionString);
    }

    // 4. Validar que tengamos una cadena de conexi�n
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("No se pudo obtener la cadena de conexi�n de ninguna fuente.");
    }

    // 5. Loggear la configuraci�n (sin contrase�a)
    var safeConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Password = "***"
    }.ToString();

    Console.WriteLine($"\n=== MODO: {env.EnvironmentName} ===");
    Console.WriteLine($"=== CADENA DE CONEXI�N UTILIZADA ===");
    Console.WriteLine(safeConnectionString);
    Console.WriteLine("==================================\n");

    return connectionString;
}

static string ConvertPostgresUrlToConnectionString(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');

    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = userInfo[0],
        Password = userInfo[1],
        SslMode = SslMode.Require,
        TrustServerCertificate = true
    }.ToString();
}

static void ConfigureServices(IServiceCollection services, string connectionString)
{
    // 1. Configurar la conexi�n a la base de datos
    services.AddScoped(_ =>
    {
        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return connection;
    });

    // 2. Configurar servicios de la aplicaci�n
    services.AddControllers();

    // 3. Configurar Swagger
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

    // 2. Configuraci�n com�n para todos los entornos
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();

    // 3. Configurar CORS (ajustar seg�n necesidades)
    app.UseCors(policy =>
        policy.WithOrigins("http://localhost:3000", "https://tudominio.com")
              .AllowAnyMethod()
              .AllowAnyHeader());

    // 4. Configurar endpoints
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();

        // Health check endpoint
        endpoints.MapGet("/health", () => "Healthy");
    });

    // 5. Redirecci�n a Swagger en desarrollo
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }
}