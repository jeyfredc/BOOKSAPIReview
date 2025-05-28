using BooksAPIReviews.Models;
using BooksAPIReviews.Models.DTOs;
using Npgsql;
using System.Data;

namespace BooksAPIReviews.Models.DAO
{
    public class UserDao 
    {
        private readonly NpgsqlConnection _connection;
        private readonly ILogger<BookDao> _logger;

        // Inyectamos la conexión directamente
        public UserDao(NpgsqlConnection _connection, ILogger<BookDao> logger)
        {
            _connection = _connection ?? throw new ArgumentNullException(nameof(_connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Verificar la conexión
            if (string.IsNullOrEmpty(_connection.ConnectionString))
            {
                _logger.LogError("La cadena de conexión está vacía");
                throw new InvalidOperationException("La cadena de conexión no está configurada");
            }

            _logger.LogInformation("BookDao inicializado con la cadena: {0}",
                new NpgsqlConnectionStringBuilder(_connection.ConnectionString) { Password = "***" });
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
        {
            var users = new List<UserResponseDto>();

            try
            {

                    await _connection.OpenAsync();
                    var query = "SELECT id, email, username, first_name, last_name, created_at FROM users";

                    using (var command = new NpgsqlCommand(query, _connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(MapToUserResponse(reader));
                        }
                    }
                
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los usuarios");
                throw;
            }
        }

        public async Task<UserResponseDto?> GetByIdAsync(Guid id)
        {
            try
            {

                    await _connection.OpenAsync();
                    var query = "SELECT id, email, username, first_name, last_name, created_at FROM users WHERE id = @id";

                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapToUserResponse(reader);
                            }
                        }
                    }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el usuario con ID: {id}");
                throw;
            }
        }

        public async Task<UserResponseDto> CreateAsync(UserCreateDto userDto, string passwordHash)
        {
            try
            {
           
                    await _connection.OpenAsync();

                    var insertQuery = @"
                        INSERT INTO users (email, username, first_name, last_name, password_hash, created_at)
                        VALUES (@email, @username, @firstName, @lastName, @passwordHash, @createdAt)
                        RETURNING id, created_at";

                    using (var command = new NpgsqlCommand(insertQuery, _connection))
                    {
                        command.Parameters.AddWithValue("email", userDto.Email);
                        command.Parameters.AddWithValue("username", userDto.Username);
                        command.Parameters.AddWithValue("firstName", (object)userDto.FirstName ?? DBNull.Value);
                        command.Parameters.AddWithValue("lastName", (object)userDto.LastName ?? DBNull.Value);
                        command.Parameters.AddWithValue("passwordHash", passwordHash);
                        command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var userId = reader.GetGuid(0);
                                var createdAt = reader.GetDateTime(1);

                                return new UserResponseDto
                                {
                                    Id = userId,
                                    Email = userDto.Email,
                                    Username = userDto.Username,
                                    FirstName = userDto.FirstName,
                                    LastName = userDto.LastName,
                                    CreatedAt = createdAt
                                };
                            }
                        }
                    
                }
                throw new Exception("No se pudo crear el usuario");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el usuario");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Guid id, UserCreateDto userDto)
        {
            // Implementar según sea necesario
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            // Implementar según sea necesario
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
           
                    await _connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM users WHERE id = @id";

                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("id", id);
                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count > 0;
                    }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si existe el usuario con ID: {id}");
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
               
                    await _connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM users WHERE email = @email";

                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("email", email);
                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count > 0;
                    }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si el email existe: {email}");
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
              
                    await _connection.OpenAsync();
                    var query = "SELECT COUNT(*) FROM users WHERE username = @username";

                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("username", username);
                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count > 0;
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si el nombre de usuario existe: {username}");
                throw;
            }
        }

        private static UserResponseDto MapToUserResponse(NpgsqlDataReader reader)
        {
            return new UserResponseDto
            {
                Id = reader.GetGuid(0),
                Email = reader.GetString(1),
                Username = reader.GetString(2),
                FirstName = reader.IsDBNull(3) ? null : reader.GetString(3),
                LastName = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = reader.GetDateTime(5)
            };
        }

        public async Task<UserResponseDto?> AuthenticateAsync(string usernameOrEmail, string password)
        {
            const string query = @"
        SELECT 
            id, 
            email, 
            username, 
            first_name, 
            last_name, 
            password_hash,
            created_at
        FROM users 
        WHERE username = @usernameOrEmail OR email = @usernameOrEmail
        LIMIT 1";

            try
            {
            
                    await _connection.OpenAsync();

                    using (var command = new NpgsqlCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("usernameOrEmail", usernameOrEmail);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var user = new UserWithPassword
                                {
                                    Id = reader.GetGuid(0),
                                    Email = reader.GetString(1),
                                    Username = reader.GetString(2),
                                    FirstName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    LastName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    PasswordHash = reader.GetString(5), // Asumiendo que es string, si es byte[] usa GetFieldValue<byte[]>(5)
                                    CreatedAt = reader.GetDateTime(6)
                                };

                                // Verificar la contraseña
                                if (VerifyPassword(password, user.PasswordHash))
                                {
                                    return new UserResponseDto
                                    {
                                        Id = user.Id,
                                        Email = user.Email,
                                        Username = user.Username,
                                        FirstName = user.FirstName,
                                        LastName = user.LastName,
                                        CreatedAt = user.CreatedAt
                                    };
                                }
                            }
                       }
                    
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al autenticar usuario");
                throw;
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            // Implementa la lógica de verificación de contraseña
            // Esto es un ejemplo básico, asegúrate de usar el mismo método de hashing que usaste al crear el usuario
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                // Aquí deberías usar la misma lógica que usaste para hashear la contraseña originalmente
                // Por ejemplo, si usaste BCrypt:
                // return BCrypt.Net.BCrypt.Verify(password, storedHash);

                // O si usaste otro método de hashing, asegúrate de que coincida
                // Por ahora, asumiré que es una comparación directa (NO RECOMENDADO para producción)
                var computedHash = HashPassword(password);
                return computedHash == storedHash;
            }
        }

        private string HashPassword(string password)
        {
            // Implementa el mismo método de hashing que usaste al crear el usuario
            // Ejemplo con BCrypt (recomendado):
            // return BCrypt.Net.BCrypt.HashPassword(password);

            // Ejemplo básico (NO USAR EN PRODUCCIÓN):
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Clase auxiliar para manejar usuarios con contraseña
        private class UserWithPassword : UserResponseDto
        {
            public string PasswordHash { get; set; } = string.Empty;
        }
    }
}