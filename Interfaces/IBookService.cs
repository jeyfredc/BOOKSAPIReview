using BooksAPIReviews.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public interface IBookService
    {
        Task<IEnumerable<BookResponseDto>> GetBooksAsync();
        Task<BookResponseDto> GetBookByIdAsync(Guid id);

        Task<bool> BookExistsAsync(Guid id);
    }
}