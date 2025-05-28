using BooksAPIReviews.Interfaces;
using BooksAPIReviews.Models;
using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

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


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BooksAPIReviews", Version = "v1" });
});

// Configuración de la base de datos
builder.Services.AddScoped<NpgsqlConnection>(sp =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BooksAPIReviews v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");

