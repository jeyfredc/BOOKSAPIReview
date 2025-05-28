using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Models.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly CategoryDao _categoryDao;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(CategoryDao categoryDao, ILogger<CategoryService> logger)
        {
            _categoryDao = categoryDao ?? throw new ArgumentNullException(nameof(categoryDao));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            try
            {
                return await _categoryDao.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las categorías");
                throw;
            }
        }

        public async Task<CategoryDto> GetCategoryByIdAsync(int id)
        {
            try
            {
                return await _categoryDao.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener la categoría con ID: {id}");
                throw;
            }
        }

        public async Task<CategoryDto> CreateCategoryAsync(CategoryCreateDto categoryDto)
        {
            try
            {
                // Verificar si ya existe una categoría con el mismo nombre
                var categoryExists = await _categoryDao.ExistsByNameAsync(categoryDto.Name);
                if (categoryExists)
                {
                    throw new InvalidOperationException($"Ya existe una categoría con el nombre: {categoryDto.Name}");
                }

                return await _categoryDao.CreateAsync(categoryDto);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la categoría");
                throw new Exception("Ocurrió un error al crear la categoría", ex);
            }
        }

        public async Task<bool> UpdateCategoryAsync(int id, CategoryUpdateDto categoryDto)
        {
            try
            {
                // Verificar si la categoría existe
                var categoryExists = await _categoryDao.ExistsAsync(id);
                if (!categoryExists)
                {
                    return false;
                }

                // Verificar si ya existe otra categoría con el mismo nombre
                var nameExists = await _categoryDao.ExistsByNameAsync(categoryDto.Name, id);
                if (nameExists)
                {
                    throw new InvalidOperationException($"Ya existe una categoría con el nombre: {categoryDto.Name}");
                }

                return await _categoryDao.UpdateAsync(id, categoryDto);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar la categoría con ID: {id}");
                throw new Exception("Ocurrió un error al actualizar la categoría", ex);
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                // Verificar si la categoría existe
                var categoryExists = await _categoryDao.ExistsAsync(id);
                if (!categoryExists)
                {
                    return false;
                }

                // Verificar si hay libros asociados a esta categoría
                var hasBooks = await _categoryDao.HasBooksAsync(id);
                if (hasBooks)
                {
                    throw new InvalidOperationException("No se puede eliminar la categoría porque tiene libros asociados");
                }

                return await _categoryDao.DeleteAsync(id);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar la categoría con ID: {id}");
                throw new Exception("Ocurrió un error al eliminar la categoría", ex);
            }
        }
    }
}