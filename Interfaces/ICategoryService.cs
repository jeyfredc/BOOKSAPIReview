using BooksAPIReviews.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto> GetCategoryByIdAsync(int id);
        Task<CategoryDto> CreateCategoryAsync(CategoryCreateDto categoryDto);
        Task<bool> UpdateCategoryAsync(int id, CategoryUpdateDto categoryDto);
        Task<bool> DeleteCategoryAsync(int id);
    }
}