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
        Task<BookResponseDto> CreateBookAsync(BookCreateDto bookDto);
        Task<bool> UpdateBookAsync(Guid id, BookCreateDto bookDto);
        Task<bool> DeleteBookAsync(Guid id);
        Task<bool> BookExistsAsync(Guid id);
    }
}