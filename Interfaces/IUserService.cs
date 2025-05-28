using BooksAPIReviews.Models;
using BooksAPIReviews.Models.DTOs;

namespace BooksAPIReviews.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(Guid id);
        Task<bool> UpdateUserAsync(Guid id, UserCreateDto userDto);
        Task<bool> DeleteUserAsync(Guid id);
        Task<bool> UserExistsAsync(Guid id);
    }
}