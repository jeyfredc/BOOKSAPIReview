using BooksAPIReviews.Models.DTOs;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BooksAPIReviews.Models.DAO
{
    public class BookDao : IDisposable, IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly ILogger<BookDao> _logger;
        private bool _disposed = false;

        public BookDao(NpgsqlConnection connection, ILogger<BookDao> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(_connection.ConnectionString))
            {
                _logger.LogError("La cadena de conexión está vacía");
                throw new InvalidOperationException("La cadena de conexión no está configurada");
            }

            _logger.LogInformation("BookDao inicializado con la cadena: {0}",
                new NpgsqlConnectionStringBuilder(_connection.ConnectionString) { Password = "***" });
        }

        private async Task EnsureConnectionOpenAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
        }

        public async Task<IEnumerable<BookResponseDto>> GetAllAsync()
        {
            var books = new List<BookResponseDto>();

            try
            {
                await EnsureConnectionOpenAsync();

                var query = @"
                SELECT 
                    id, 
                    title, 
                    author, 
                    description, 
                    cover_image_url, 
                    published_date, 
                    average_rating, 
                    review_count, 
                    created_at, 
                    updated_at
                FROM books";

                using (var command = new NpgsqlCommand(query, _connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        try
                        {
                            books.Add(new BookResponseDto
                            {
                                Id = reader.GetGuid(0),
                                Title = reader.GetString(1),
                                Author = reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                CoverImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                                PublishedDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                AverageRating = reader.GetDecimal(6),
                                ReviewCount = reader.GetInt32(7),
                                CreatedAt = reader.GetDateTime(8),
                                UpdatedAt = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9)
                            });
                        }
                        catch (Exception ex)
                        {
                            var fieldData = new List<string>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                fieldData.Add($"{reader.GetName(i)}: {(reader.IsDBNull(i) ? "NULL" : reader[i])}");
                            }
                            _logger.LogError(ex, "Error al mapear el libro. Datos: {Data}", string.Join(", ", fieldData));
                            throw;
                            throw;
                        }
                    }
                }

                return books;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los libros");
                throw;
            }
        }

        public async Task<BookResponseDto> GetByIdAsync(Guid id)
        {
            try
            {
                await EnsureConnectionOpenAsync();

                var query = @"
                    SELECT 
                        id, title, author, description, cover_image_url, 
                        published_date, category, average_rating, 
                        review_count, created_at, updated_at 
                    FROM books 
                    WHERE id = @id";

                using (var command = new NpgsqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("id", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new BookResponseDto
                            {
                                Id = reader.GetGuid(0),
                                Title = reader.GetString(1),
                                Author = reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                CoverImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                                PublishedDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                Category = reader.IsDBNull(6) ? null : reader.GetString(6),
                                AverageRating = reader.GetDecimal(7),
                                ReviewCount = reader.GetInt32(8),
                                CreatedAt = reader.GetDateTime(9),
                                UpdatedAt = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10)
                            };
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el libro con ID: {Id}", id);
                throw;
            }
        }

        public async Task<BookResponseDto> CreateAsync(BookCreateDto bookDto)
        {
            try
            {
                await EnsureConnectionOpenAsync();

                var insertQuery = @"
                    INSERT INTO books (
                        title, author, description, cover_image_url, 
                        published_date, category, created_at
                    ) 
                    VALUES (
                        @title, @author, @description, @coverImageUrl, 
                        @publishedDate, @category, @createdAt
                    )
                    RETURNING 
                        id, average_rating, review_count, created_at, updated_at";

                using (var command = new NpgsqlCommand(insertQuery, _connection))
                {
                    command.Parameters.AddWithValue("title", bookDto.Title);
                    command.Parameters.AddWithValue("author", bookDto.Author);
                    command.Parameters.AddWithValue("description", (object)bookDto.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("coverImageUrl", (object)bookDto.CoverImageUrl ?? DBNull.Value);
                    command.Parameters.AddWithValue("publishedDate", (object)bookDto.PublishedDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("category", (object)bookDto.Category ?? DBNull.Value);
                    command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new BookResponseDto
                            {
                                Id = reader.GetGuid(0),
                                Title = bookDto.Title,
                                Author = bookDto.Author,
                                Description = bookDto.Description,
                                CoverImageUrl = bookDto.CoverImageUrl,
                                PublishedDate = bookDto.PublishedDate,
                                Category = bookDto.Category,
                                AverageRating = reader.GetDecimal(1),
                                ReviewCount = reader.GetInt32(2),
                                CreatedAt = reader.GetDateTime(3),
                                UpdatedAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4)
                            };
                        }
                    }
                }

                throw new Exception("No se pudo crear el libro");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el libro");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Guid id, BookCreateDto bookDto)
        {
            try
            {
                await EnsureConnectionOpenAsync();

                var query = @"
                    UPDATE books 
                    SET 
                        title = @title,
                        author = @author,
                        description = @description,
                        cover_image_url = @coverImageUrl,
                        published_date = @publishedDate,
                        category = @category,
                        updated_at = @updatedAt
                    WHERE id = @id";

                using (var command = new NpgsqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("id", id);
                    command.Parameters.AddWithValue("title", bookDto.Title);
                    command.Parameters.AddWithValue("author", bookDto.Author);
                    command.Parameters.AddWithValue("description", (object)bookDto.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("coverImageUrl", (object)bookDto.CoverImageUrl ?? DBNull.Value);
                    command.Parameters.AddWithValue("publishedDate", (object)bookDto.PublishedDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("category", (object)bookDto.Category ?? DBNull.Value);
                    command.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el libro con ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                await EnsureConnectionOpenAsync();

                var query = "DELETE FROM books WHERE id = @id";
                using (var command = new NpgsqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("id", id);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el libro con ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                await EnsureConnectionOpenAsync();

                var query = "SELECT COUNT(*) FROM books WHERE id = @id";
                using (var command = new NpgsqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("id", id);
                    var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar si existe el libro con ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> TitleExistsAsync(string title)
        {
            try
            {
                await EnsureConnectionOpenAsync();

                var query = "SELECT COUNT(*) FROM books WHERE LOWER(title) = LOWER(@title)";
                using (var command = new NpgsqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("title", title);
                    var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar si existe un libro con el título: {Title}", title);
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