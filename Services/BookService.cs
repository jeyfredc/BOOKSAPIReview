using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Models.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public class BookService : IBookService
    {
        private readonly BookDao _bookDao;
        private readonly ILogger<BookService> _logger;

        public BookService(BookDao bookDao, ILogger<BookService> logger)
        {
            _bookDao = bookDao ?? throw new ArgumentNullException(nameof(bookDao));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<BookResponseDto>> GetBooksAsync()
        {
            try
            {
                return await _bookDao.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los libros");
                throw;
            }
        }

        public async Task<BookResponseDto> GetBookByIdAsync(Guid id)
        {
            try
            {
                return await _bookDao.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el libro con ID: {id}");
                throw;
            }
        }

        public async Task<BookResponseDto> CreateBookAsync(BookCreateDto bookDto)
        {
            try
            {
                if (await _bookDao.TitleExistsAsync(bookDto.Title))
                {
                    throw new InvalidOperationException("Ya existe un libro con el mismo título");
                }

                return await _bookDao.CreateAsync(bookDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el libro");
                throw;
            }
        }

        public async Task<bool> UpdateBookAsync(Guid id, BookCreateDto bookDto)
        {
            try
            {
                var bookExists = await _bookDao.ExistsAsync(id);
                if (!bookExists)
                {
                    return false;
                }

                return await _bookDao.UpdateAsync(id, bookDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el libro con ID: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteBookAsync(Guid id)
        {
            try
            {
                var bookExists = await _bookDao.ExistsAsync(id);
                if (!bookExists)
                {
                    return false;
                }

                return await _bookDao.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el libro con ID: {id}");
                throw;
            }
        }

        public async Task<bool> BookExistsAsync(Guid id)
        {
            try
            {
                return await _bookDao.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar si existe el libro con ID: {id}");
                throw;
            }
        }
    }
}