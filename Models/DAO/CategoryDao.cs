// En Models/DAO/CategoryDao.cs
using BooksAPIReviews.Models.DTOs;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace BooksAPIReviews.Models.DAO
{
    public class CategoryDao 
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CategoryDao> _logger;

        public CategoryDao(IConfiguration configuration, ILogger<CategoryDao> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var categories = new List<CategoryDto>();

            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = @"
                        SELECT 
                            id, 
                            title, 
                            author,
                            description, 
                            created_at
                        FROM books
                        ORDER BY author";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categories.Add(MapToCategoryDto(reader));
                        }
                    }
                }
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las categorías");
                throw;
            }
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = @"
                        SELECT 
                            id, 
                            name, 
                            description, 
                            created_at, 
                            updated_at
                        FROM categories 
                        WHERE id = @id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapToCategoryDto(reader);
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener la categoría con ID: {id}");
                throw;
            }
        }

        public async Task<CategoryDto> CreateAsync(CategoryCreateDto categoryDto)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    var insertQuery = @"
                        INSERT INTO categories (
                            name, 
                            description, 
                            created_at
                        )
                        VALUES (
                            @name, 
                            @description, 
                            @createdAt
                        )
                        RETURNING id, created_at, updated_at";

                    using (var command = new NpgsqlCommand(insertQuery, connection))
                    {
                        var now = DateTime.UtcNow;

                        command.Parameters.AddWithValue("name", categoryDto.Name);
                        command.Parameters.AddWithValue("description", (object)categoryDto.Description ?? DBNull.Value);
                        command.Parameters.AddWithValue("createdAt", now);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new CategoryDto
                                {
                                    Id = reader.GetInt32(0),
                                    Name = categoryDto.Name,
                                    Description = categoryDto.Description,
                                    CreatedAt = reader.GetDateTime(1),
                                    UpdatedAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2)
                                };
                            }
                        }
                    }
                }
                throw new Exception("No se pudo crear la categoría");
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505") // Violación de restricción única
            {
                _logger.LogWarning(ex, $"Ya existe una categoría con el nombre: {categoryDto.Name}");
                throw new InvalidOperationException($"Ya existe una categoría con el nombre: {categoryDto.Name}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la categoría");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, CategoryUpdateDto categoryDto)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    var updateQuery = @"
                        UPDATE categories 
                        SET 
                            name = @name, 
                            description = @description, 
                            updated_at = @updatedAt
                        WHERE id = @id";

                    using (var command = new NpgsqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("id", id);
                        command.Parameters.AddWithValue("name", categoryDto.Name);
                        command.Parameters.AddWithValue("description", (object)categoryDto.Description ?? DBNull.Value);
                        command.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505") // Violación de restricción única
            {
                _logger.LogWarning(ex, $"Ya existe una categoría con el nombre: {categoryDto.Name}");
                throw new InvalidOperationException($"Ya existe una categoría con el nombre: {categoryDto.Name}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar la categoría con ID: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    var deleteQuery = "DELETE FROM categories WHERE id = @id";

                    using (var command = new NpgsqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("id", id);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503") // Violación de clave foránea
            {
                _logger.LogWarning(ex, $"No se puede eliminar la categoría con ID: {id} porque tiene libros asociados");
                throw new InvalidOperationException("No se puede eliminar la categoría porque tiene libros asociados", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar la categoría con ID: {id}");
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM categories WHERE id = @id";

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
                _logger.LogError(ex, $"Error al verificar si existe la categoría con ID: {id}");
                throw;
            }
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    var query = "SELECT COUNT(*) FROM categories WHERE name = @name";
                    if (excludeId.HasValue)
                    {
                        query += " AND id != @excludeId";
                    }

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("name", name);
                        if (excludeId.HasValue)
                        {
                            command.Parameters.AddWithValue("excludeId", excludeId.Value);
                        }

                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si existe la categoría con nombre: {name}");
                throw;
            }
        }

        public async Task<bool> HasBooksAsync(int categoryId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM books WHERE category_id = @categoryId";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("categoryId", categoryId);
                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si la categoría con ID: {categoryId} tiene libros asociados");
                throw;
            }
        }

        private static CategoryDto MapToCategoryDto(NpgsqlDataReader reader)
        {
            return new CategoryDto
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = reader.GetDateTime(3),
                UpdatedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
            };
        }
    }
}