using BooksAPIReviews.Models.DAO;
using BooksAPIReviews.Models.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public class SearchService : ISearchService
    {
        private readonly SearchDao _searchDao;
        private readonly ILogger<SearchService> _logger;

        public SearchService(SearchDao searchDao, ILogger<SearchService> logger)
        {
            _searchDao = searchDao ?? throw new ArgumentNullException(nameof(searchDao));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SearchResultDto<BookSearchResultDto>> SearchBooksAsync(string query, int page, int pageSize)
        {
            try
            {
                var (books, totalCount) = await _searchDao.SearchBooksAsync(query, page, pageSize);
                
                return new SearchResultDto<BookSearchResultDto>
                {
                    Items = books,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al buscar libros con el término: {query}");
                throw;
            }
        }
    }
}