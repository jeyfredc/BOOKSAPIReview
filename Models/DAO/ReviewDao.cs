using BooksAPIReviews.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Models.DAO
{
    public class ReviewDao 
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReviewDao> _logger;

        public ReviewDao(IConfiguration configuration, ILogger<ReviewDao> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IEnumerable<ReviewResponseDto>> GetAllAsync()
        {
            var reviews = new List<ReviewResponseDto>();

            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = @"
                        SELECT r.id, r.book_id, b.title as book_title, 
                               r.user_id, u.username as user_name, 
                               r.rating, r.comment, r.created_at, r.updated_at
                        FROM reviews r
                        JOIN books b ON r.book_id = b.id
                        JOIN users u ON r.user_id = u.id";

                    using (var command = new NpgsqlCommand(query, connection))
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
                _logger.LogError(ex, "Error al obtener todas las reseñas");
                throw;
            }
        }

        public async Task<ReviewResponseDto> GetByIdAsync(Guid id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = @"
                        SELECT r.id, r.book_id, b.title as book_title, 
                               r.user_id, u.username as user_name, 
                               r.rating, r.comment, r.created_at, r.updated_at
                        FROM reviews r
                        JOIN books b ON r.book_id = b.id
                        JOIN users u ON r.user_id = u.id
                        WHERE r.id = @id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapToReviewResponse(reader);
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener la reseña con ID: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<ReviewResponseDto>> GetByBookIdAsync(Guid bookId)
        {
            var reviews = new List<ReviewResponseDto>();

            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = @"
                        SELECT r.id, r.book_id, b.title as book_title, 
                               r.user_id, u.username as user_name, 
                               r.rating, r.comment, r.created_at, r.updated_at
                        FROM reviews r
                        JOIN books b ON r.book_id = b.id
                        JOIN users u ON r.user_id = u.id
                        WHERE r.book_id = @bookId
                        ORDER BY r.created_at DESC";

                    using (var command = new NpgsqlCommand(query, connection))
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
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = @"
                        SELECT r.id, r.book_id, b.title as book_title, 
                               r.user_id, u.username as user_name, 
                               r.rating, r.comment, r.created_at, r.updated_at
                        FROM reviews r
                        JOIN books b ON r.book_id = b.id
                        JOIN users u ON r.user_id = u.id
                        WHERE r.user_id = @userId
                        ORDER BY r.created_at DESC";

                    using (var command = new NpgsqlCommand(query, connection))
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
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    using (var transaction = await connection.BeginTransactionAsync())
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

                            using (var command = new NpgsqlCommand(insertQuery, connection, transaction))
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
                            await UpdateBookRatingAsync(connection, transaction, reviewDto.BookId);

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
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    using (var transaction = await connection.BeginTransactionAsync())
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

                            using (var command = new NpgsqlCommand(query, connection, transaction))
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
                                        await UpdateBookRatingAsync(connection, transaction, bookId);

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
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // 1. Obtener el book_id antes de eliminar
                            var bookIdQuery = "SELECT book_id FROM reviews WHERE id = @id";
                            Guid bookId;

                            using (var command = new NpgsqlCommand(bookIdQuery, connection, transaction))
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

                            using (var command = new NpgsqlCommand(deleteQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("id", id);
                                int rowsAffected = await command.ExecuteNonQueryAsync();

                                if (rowsAffected == 0)
                                {
                                    return false;
                                }
                            }

                            // 3. Actualizar el promedio de calificaciones del libro
                            await UpdateBookRatingAsync(connection, transaction, bookId);

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
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM reviews WHERE id = @id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("id", id);
                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count > 0;
                    }
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
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM reviews WHERE user_id = @userId AND book_id = @bookId";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("userId", userId);
                        command.Parameters.AddWithValue("bookId", bookId);
                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si el usuario {userId} ya revisó el libro {bookId}");
                throw;
            }
        }

        private async Task UpdateBookRatingAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid bookId)
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

            using (var command = new NpgsqlCommand(updateBookQuery, connection, transaction))
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
    }
}