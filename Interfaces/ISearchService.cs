using BooksAPIReviews.Models.DTOs;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public interface ISearchService
    {
        Task<SearchResultDto<BookSearchResultDto>> SearchBooksAsync(string query, int page, int pageSize);

    }
}