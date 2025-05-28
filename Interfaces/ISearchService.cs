using BooksAPIReviews.Models.DTOs;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public interface ISearchService
    {
        Task<SearchResultDto<BookSearchResultDto>> SearchBooksAsync(string query, int page, int pageSize);
        Task<SearchResultDto<BookSearchResultDto>> SearchBooksByCategoryAsync(string category, int page, int pageSize);
        Task<SearchResultDto<ReviewSearchResultDto>> SearchReviewsAsync(string query, int? minRating, int? maxRating, int page, int pageSize);
    }
}