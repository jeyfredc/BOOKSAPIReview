using BooksAPIReviews.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Models.DAO
{
    public class ReviewDao : IDisposable, IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly ILogger<ReviewDao> _logger;
        private bool _disposed = false;

        public ReviewDao(NpgsqlConnection connection, ILogger<ReviewDao> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(_connection.ConnectionString))
            {
                _logger.LogError("La cadena de conexión está vacía");
                throw new InvalidOperationException("La cadena de conexión no está configurada");
            }

            _logger.LogInformation("ReviewDao inicializado con la cadena: {0}",
                new NpgsqlConnectionStringBuilder(_connection.ConnectionString) { Password = "***" });
        }

        private async Task EnsureConnectionOpenAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await EnsureConnectionOpenAsync();
            }
        }

        public async Task<IEnumerable<ReviewResponseDto>> GetAllAsync()
        {
            var reviews = new List<ReviewResponseDto>();

            try
            {

                    await EnsureConnectionOpenAsync();
                    var query = @"
                        SELECT r.id, r.book_id, b.title as book_title, 
                               r.user_id, u.username as user_name, 
                               r.rating, r.comment, r.created_at, r.updated_at
                        FROM reviews r
                        JOIN books b ON r.book_id = b.id
                        JOIN users u ON r.user_id = u.id";

                    using (var command = new NpgsqlCommand(query, _connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            reviews.Add(MapToReviewResponse(reader));
                        }
                    }
                
                return reviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las reseñas");
                throw;
            }
        }

        public async Task<List<ReviewResponseDto>> GetByIdAsync(Guid id)
        {
            var reviews = new List<ReviewResponseDto>();

            try
            {
                await EnsureConnectionOpenAsync();
                var query = @"
                    SELECT r.id, r.book_id, b.title as book_title, 
                    r.user_id, u.username as user_name, 
                    r.rating, r.comment, r.created_at, r.updated_at
                    FROM reviews r
                    JOIN books b ON r.book_id = b.id
                    JOIN users u ON r.user_id = u.id
                    WHERE b.id = @id
                    ORDER BY r.created_at DESC";  

                using (var command = new NpgsqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("id", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var review = MapToReviewResponse(reader);
                            if (review != null)
                            {
                                reviews.Add(review);
                            }
                        }
                    }
                }

                return reviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener las reseñas para el libro con ID: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<ReviewResponseDto>> GetByBookIdAsync(Guid bookId)
        {
            var reviews = new List<ReviewResponseDto>();

            try
            {
        
                    await EnsureConnectionOpenAsync();
                    var query = @"
                        SELECT r.id, r.book_id, b.title as book_title, 
                               r.user_id, u.username as user_name, 
                               r.rating, r.comment, r.created_at, r.updated_at
                        FROM reviews r
                        JOIN books b ON r.book_id = b.id
                        JOIN users u ON r.user_id = u.id
                        WHERE r.book_id = @bookId
                        ORDER BY r.created_at DESC";

                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("bookId", bookId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                reviews.Add(MapToReviewResponse(reader));
                            }
                        }
                    
                }
                return reviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener las reseñas del libro con ID: {bookId}");
                throw;
            }
        }

        public async Task<IEnumerable<ReviewResponseDto>> GetByUserIdAsync(Guid userId)
        {
            var reviews = new List<ReviewResponseDto>();

            try
            {
        
                    await EnsureConnectionOpenAsync();
                    var query = @"
                        SELECT r.id, r.book_id, b.title as book_title, 
                               r.user_id, u.username as user_name, 
                               r.rating, r.comment, r.created_at, r.updated_at
                        FROM reviews r
                        JOIN books b ON r.book_id = b.id
                        JOIN users u ON r.user_id = u.id
                        WHERE r.user_id = @userId
                        ORDER BY r.created_at DESC";

                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("userId", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                reviews.Add(MapToReviewResponse(reader));
                            }
                        }
                    }
                
                return reviews;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener las reseñas del usuario con ID: {userId}");
                throw;
            }
        }

        public async Task<ReviewResponseDto> CreateAsync(ReviewCreateDto reviewDto)
        {
            try
            {
             
                    await EnsureConnectionOpenAsync();

                    using (var transaction = await _connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // 1. Primero leemos el ID del libro para usarlo después
                            var bookId = reviewDto.BookId;

                            // 2. Insertar la reseña
                            var insertQuery = @"
                        INSERT INTO reviews (
                            book_id, user_id, rating, comment, created_at
                        ) 
                        VALUES (
                            @bookId, @userId, @rating, @comment, @createdAt
                        )
                        RETURNING id, created_at, updated_at";

                            Guid reviewId;
                            DateTime createdAt;
                            DateTime? updatedAt;

                            using (var command = new NpgsqlCommand(insertQuery, _connection, transaction))
                            {
                                command.Parameters.AddWithValue("bookId", reviewDto.BookId);
                                command.Parameters.AddWithValue("userId", reviewDto.UserId);
                                command.Parameters.AddWithValue("rating", reviewDto.Rating);
                                command.Parameters.AddWithValue("comment", (object)reviewDto.Comment ?? DBNull.Value);
                                command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    if (!await reader.ReadAsync())
                                    {
                                        throw new Exception("No se pudo crear la reseña");
                                    }

                                    // Leer los valores del DataReader
                                    reviewId = reader.GetGuid(0);
                                    createdAt = reader.GetDateTime(1);
                                    updatedAt = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
                                } // El DataReader se cierra aquí
                            }

                            // 3. Ahora que el DataReader está cerrado, podemos ejecutar otra consulta
                            await UpdateBookRatingAsync(_connection, transaction, reviewDto.BookId);

                            // 4. Confirmar la transacción
                            await transaction.CommitAsync();

                            // 5. Devolver la respuesta
                            return new ReviewResponseDto
                            {
                                Id = reviewId,
                                BookId = reviewDto.BookId,
                                UserId = reviewDto.UserId,
                                Rating = reviewDto.Rating,
                                Comment = reviewDto.Comment,
                                CreatedAt = createdAt,
                                UpdatedAt = updatedAt
                            };
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la reseña");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Guid id, ReviewCreateDto reviewDto)
        {
            try
            {
                    await EnsureConnectionOpenAsync();

                    using (var transaction = await _connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // 1. Actualizar la reseña
                            var query = @"
                                UPDATE reviews 
                                SET 
                                    rating = @rating,
                                    comment = @comment,
                                    updated_at = @updatedAt
                                WHERE id = @id
                                RETURNING book_id";

                            using (var command = new NpgsqlCommand(query, _connection, transaction))
                            {
                                command.Parameters.AddWithValue("id", id);
                                command.Parameters.AddWithValue("rating", reviewDto.Rating);
                                command.Parameters.AddWithValue("comment", (object)reviewDto.Comment ?? DBNull.Value);
                                command.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        var bookId = reader.GetGuid(0);

                                        // 2. Actualizar el promedio de calificaciones del libro
                                        await UpdateBookRatingAsync(_connection, transaction, bookId);

                                        await transaction.CommitAsync();
                                        return true;
                                    }
                                }
                            }
                            return false;
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar la reseña con ID: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
           
                    await EnsureConnectionOpenAsync();

                    using (var transaction = await _connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // 1. Obtener el book_id antes de eliminar
                            var bookIdQuery = "SELECT book_id FROM reviews WHERE id = @id";
                            Guid bookId;

                            using (var command = new NpgsqlCommand(bookIdQuery, _connection, transaction))
                            {
                                command.Parameters.AddWithValue("id", id);
                                var result = await command.ExecuteScalarAsync();

                                if (result == null)
                                {
                                    return false; // La reseña no existe
                                }

                                bookId = (Guid)result;
                            }

                            // 2. Eliminar la reseña
                            var deleteQuery = "DELETE FROM reviews WHERE id = @id";

                            using (var command = new NpgsqlCommand(deleteQuery, _connection, transaction))
                            {
                                command.Parameters.AddWithValue("id", id);
                                int rowsAffected = await command.ExecuteNonQueryAsync();

                                if (rowsAffected == 0)
                                {
                                    return false;
                                }
                            }

                            // 3. Actualizar el promedio de calificaciones del libro
                            await UpdateBookRatingAsync(_connection, transaction, bookId);

                            await transaction.CommitAsync();
                            return true;
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar la reseña con ID: {id}");
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
              
                    await EnsureConnectionOpenAsync();
                    var query = "SELECT COUNT(*) FROM reviews WHERE id = @id";

                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("id", id);
                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count > 0;
                    }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si existe la reseña con ID: {id}");
                throw;
            }
        }

        public async Task<bool> UserHasReviewedBookAsync(Guid userId, Guid bookId)
        {
            try
            {
               
                    await EnsureConnectionOpenAsync();
                    var query = "SELECT COUNT(*) FROM reviews WHERE user_id = @userId AND book_id = @bookId";

                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("userId", userId);
                        command.Parameters.AddWithValue("bookId", bookId);
                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count > 0;
                    }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si el usuario {userId} ya revisó el libro {bookId}");
                throw;
            }
        }

        private async Task UpdateBookRatingAsync(NpgsqlConnection _connection, NpgsqlTransaction transaction, Guid bookId)
        {
            // Actualizar el promedio y el conteo de reseñas del libro
            var updateBookQuery = @"
                UPDATE books 
                SET 
                    average_rating = (
                        SELECT COALESCE(AVG(rating), 0)
                        FROM reviews 
                        WHERE book_id = @bookId
                    ),
                    review_count = (
                        SELECT COUNT(*) 
                        FROM reviews 
                        WHERE book_id = @bookId
                    ),
                    updated_at = @updatedAt
                WHERE id = @bookId";

            using (var command = new NpgsqlCommand(updateBookQuery, _connection, transaction))
            {
                command.Parameters.AddWithValue("bookId", bookId);
                command.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
                await command.ExecuteNonQueryAsync();
            }
        }

        private ReviewResponseDto MapToReviewResponse(NpgsqlDataReader reader)
        {
            return new ReviewResponseDto
            {
                Id = reader.GetGuid(0),
                BookId = reader.GetGuid(1),
                BookTitle = reader.GetString(2),
                UserId = reader.GetGuid(3),
                UserName = reader.GetString(4),
                Rating = reader.GetInt32(5),
                Comment = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = reader.GetDateTime(7),
                UpdatedAt = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8)
            };
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