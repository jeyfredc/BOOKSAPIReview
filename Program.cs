using BooksAPIReviews.Interfaces;
using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Configuraci�n b�sica
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BooksAPIReviews", Version = "v1" });
});

// Registrar servicios
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

// Configuraci�n de la base de datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=booksdb;Username=postgres;Password=tu_contrase�a;";

builder.Services.AddScoped<NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));

var app = builder.Build();

// Configuraci�n del pipeline
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BooksAPI v1"));
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Endpoint de verificaci�n
app.MapGet("/", () => "API de BooksAPI est� funcionando");

// Iniciar la aplicaci�n
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");