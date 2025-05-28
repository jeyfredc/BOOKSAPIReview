using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Models.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserDao _userDao;
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserDao userDao, ILogger<AuthService> logger)
        {
            _userDao = userDao ?? throw new ArgumentNullException(nameof(userDao));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthResult> RegisterAsync(UserRegisterDto registerDto)
        {
            try
            {
                // Verificar si el usuario ya existe
                var userExists = await _userDao.EmailExistsAsync(registerDto.Email) ||
                                await _userDao.UsernameExistsAsync(registerDto.Username);

                if (userExists)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "El nombre de usuario o correo electrónico ya está en uso"
                    };
                }

                var password = HashPassword(registerDto.Password);
                var user = new UserCreateDto
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    Password = password
                };

                var createdUser = await _userDao.CreateAsync(user, password);

                return new AuthResult
                {
                    Success = true,
                    Message = "Usuario registrado exitosamente",
                    UserId = createdUser.Id.ToString(),
                    Username = createdUser.Username,
                    Email = createdUser.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario");
                return new AuthResult
                {
                    Success = false,
                    Message = "Error al registrar el usuario"
                };
            }
        }

        public async Task<AuthResult> LoginAsync(UserLoginDto loginDto)
        {
            try
            {
                // Verificar credenciales
                var user = await _userDao.AuthenticateAsync(loginDto.UsernameOrEmail, loginDto.Password);

                if (user == null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Credenciales inválidas"
                    };
                }

                return new AuthResult
                {
                    Success = true,
                    Message = "Inicio de sesión exitoso",
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión");
                return new AuthResult
                {
                    Success = false,
                    Message = "Error al iniciar sesión"
                };
            }
        }

        private static string HashPassword(string password)
        {
            // En producción, usa BCrypt o similar
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}