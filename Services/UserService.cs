using BooksAPIReviews.Interfaces;
using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Models.DTOs;
using BooksAPIReviews.Services;
using Microsoft.Extensions.Logging;

namespace BooksAPIReviews.Services
{
    public class UserService : IUserService
    {
        private readonly UserDao _userDao;
        private readonly ILogger<UserService> _logger;

        public UserService(UserDao userDao, ILogger<UserService> logger)
        {
            _userDao = userDao;
            _logger = logger;
        }

        public async Task<IEnumerable<UserResponseDto>> GetUsersAsync()
        {
            try
            {
                return await _userDao.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los usuarios");
                throw;
            }
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
        {
            try
            {
                return await _userDao.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el usuario con ID: {id}");
                throw;
            }
        }


        public async Task<bool> UpdateUserAsync(Guid id, UserCreateDto userDto)
        {
            try
            {
                return await _userDao.UpdateAsync(id, userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el usuario con ID: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            try
            {
                return await _userDao.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el usuario con ID: {id}");
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(Guid id)
        {
            try
            {
                return await _userDao.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si existe el usuario con ID: {id}");
                throw;
            }
        }


    }
}