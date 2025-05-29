// En Models/DAO/SearchDao.cs
using BooksAPIReviews.Models.DTOs;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace BooksAPIReviews.Models.DAO
{
    public class SearchDao : IDisposable, IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly ILogger<SearchDao> _logger;
        private bool _disposed = false;

        public SearchDao(NpgsqlConnection connection, ILogger<SearchDao> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(_connection.ConnectionString))
            {
                _logger.LogError("La cadena de conexión está vacía");
                throw new InvalidOperationException("La cadena de conexión no está configurada");
            }

            _logger.LogInformation("UserDao inicializado con la cadena: {0}",
                new NpgsqlConnectionStringBuilder(_connection.ConnectionString) { Password = "***" });
        }

        private async Task EnsureConnectionOpenAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await EnsureConnectionOpenAsync();
            }
        }

        public async Task<(IEnumerable<BookSearchResultDto> Books, int TotalCount)> SearchBooksAsync(
            string searchTerm, int page, int pageSize)
        {
            try
            {

                    await EnsureConnectionOpenAsync();

                    // Primero obtenemos el conteo total
                    var countQuery = @"
                        SELECT COUNT(*) 
                        FROM books 
                        WHERE 
                            title ILIKE @searchTerm OR 
                            author ILIKE @searchTerm OR 
                            description ILIKE @searchTerm OR
                            category ILIKE @searchTerm";

                    int totalCount;
                    using (var countCommand = new NpgsqlCommand(countQuery, _connection))
                    {
                        countCommand.Parameters.AddWithValue("searchTerm", $"%{searchTerm}%");
                        totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync() ?? 0);
                    }

                    if (totalCount == 0)
                    {
                        return (new List<BookSearchResultDto>(), 0);
                    }

                    // Luego obtenemos los datos paginados
                    var query = @"
                        SELECT 
                            id as book_id,
                            title,
                            author,
                            description,
                            category,
                            average_rating,
                            review_count,
                            cover_image_url,
                            created_at
                        FROM books 
                        WHERE 
                            title ILIKE @searchTerm OR 
                            author ILIKE @searchTerm OR 
                            description ILIKE @searchTerm OR
                            category ILIKE @searchTerm
                        ORDER BY 
                            CASE 
                                WHEN title ILIKE @searchTerm THEN 1
                                WHEN author ILIKE @searchTerm THEN 2
                                ELSE 3
                            END,
                            title
                        LIMIT @pageSize OFFSET @offset";

                    var books = new List<BookSearchResultDto>();
                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("searchTerm", $"%{searchTerm}%");
                        command.Parameters.AddWithValue("pageSize", pageSize);
                        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                books.Add(new BookSearchResultDto
                                {
                                    Book_Id = reader.GetGuid(0),
                                    Title = reader.GetString(1),
                                    Author = reader.GetString(2),
                                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Category = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    AverageRating = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                                    ReviewCount = reader.GetInt32(6),
                                    CoverImageUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    CreatedAt = reader.GetDateTime(8)
                                });
                            }
                        }
                    }

                    return (books, totalCount);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al buscar libros con el término: {searchTerm}");
                throw;
            }
        }



        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection?.State == System.Data.ConnectionState.Open)
            {
                await _connection.CloseAsync();
            }
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}