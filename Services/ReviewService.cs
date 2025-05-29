using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Models.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ReviewDao _reviewDao;
        private readonly BookDao _bookDao;
        private readonly UserDao _userDao;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(
            ReviewDao reviewDao,
            BookDao bookDao,
            UserDao userDao,
            ILogger<ReviewService> logger)
        {
            _reviewDao = reviewDao ?? throw new ArgumentNullException(nameof(reviewDao));
            _bookDao = bookDao ?? throw new ArgumentNullException(nameof(bookDao));
            _userDao = userDao ?? throw new ArgumentNullException(nameof(userDao));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<List<ReviewResponseDto>> GetReviewByIdAsync(Guid id)
        {
            try
            {
                return await _reviewDao.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener la reseña con ID: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<ReviewResponseDto>> GetReviewsByBookIdAsync(Guid bookId)
        {
            try
            {
                // Verificar que el libro existe
                var bookExists = await _bookDao.ExistsAsync(bookId);
                if (!bookExists)
                {
                    throw new KeyNotFoundException($"No se encontró el libro con ID: {bookId}");
                }

                return await _reviewDao.GetByBookIdAsync(bookId);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener las reseñas del libro con ID: {bookId}");
                throw;
            }
        }

        public async Task<ReviewResponseDto> CreateReviewAsync(ReviewCreateDto reviewDto)
        {
            try
            {
                var bookExists = await _bookDao.ExistsAsync(reviewDto.BookId);
                if (!bookExists)
                {
                    throw new KeyNotFoundException($"No se encontró el libro con ID: {reviewDto.BookId}");
                }

                var userExists = await _userDao.ExistsAsync(reviewDto.UserId);
                if (!userExists)
                {
                    throw new KeyNotFoundException($"No se encontró el usuario con ID: {reviewDto.UserId}");
                }

                // Validar que el usuario no haya revisado ya este libro
                var hasReviewed = await _reviewDao.UserHasReviewedBookAsync(reviewDto.UserId, reviewDto.BookId);
                if (hasReviewed)
                {
                    throw new InvalidOperationException("Ya has realizado una reseña para este libro");
                }

                return await _reviewDao.CreateAsync(reviewDto);
            }
            catch (Exception ex) when (ex is not (KeyNotFoundException or InvalidOperationException))
            {
                _logger.LogError(ex, "Error al crear la reseña");
                throw;
            }
        }




        public async Task<bool> ReviewExistsAsync(Guid id)
        {
            try
            {
                return await _reviewDao.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si existe la reseña con ID: {id}");
                throw;
            }
        }
    }
}