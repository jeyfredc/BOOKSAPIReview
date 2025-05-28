using BooksAPIReviews.Models.DTOs;

namespace BooksAPIReviews.Services
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(UserRegisterDto registerDto);
        Task<AuthResult> LoginAsync(UserLoginDto loginDto);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }
}