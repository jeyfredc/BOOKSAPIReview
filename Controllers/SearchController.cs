using BooksAPIReviews.Models.DTOs;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchService searchService, ILogger<SearchController> logger)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Busca libros por término de búsqueda
        /// </summary>
        /// <param name="query">Término de búsqueda</param>
        /// <param name="page">Número de página (opcional, predeterminado: 1)</param>
        /// <param name="pageSize">Tamaño de página (opcional, predeterminado: 10)</param>
        [HttpGet("books")]
        public async Task<ActionResult<SearchResultDto<BookSearchResultDto>>> SearchBooks(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { message = "El término de búsqueda no puede estar vacío" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var result = await _searchService.SearchBooksAsync(query, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al buscar libros con el término: {query}");
                return StatusCode(500, new { message = "Ocurrió un error al realizar la búsqueda" });
            }
        }

    }
}