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

        /// <summary>
        /// Busca libros por categoría
        /// </summary>
        /// <param name="category">Nombre de la categoría</param>
        /// <param name="page">Número de página (opcional, predeterminado: 1)</param>
        /// <param name="pageSize">Tamaño de página (opcional, predeterminado: 10)</param>
        [HttpGet("books/category/{category}")]
        public async Task<ActionResult<SearchResultDto<BookSearchResultDto>>> SearchBooksByCategory(
            string category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    return BadRequest(new { message = "La categoría no puede estar vacía" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var result = await _searchService.SearchBooksByCategoryAsync(category, page, pageSize);

                if (result.TotalCount == 0)
                {
                    return NotFound(new { message = $"No se encontraron libros en la categoría: {category}" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al buscar libros en la categoría: {category}");
                return StatusCode(500, new { message = "Ocurrió un error al realizar la búsqueda" });
            }
        }

        /// <summary>
        /// Busca reseñas por término de búsqueda
        /// </summary>
        /// <param name="query">Término de búsqueda</param>
        /// <param name="minRating">Puntuación mínima (opcional)</param>
        /// <param name="maxRating">Puntuación máxima (opcional)</param>
        /// <param name="page">Número de página (opcional, predeterminado: 1)</param>
        /// <param name="pageSize">Tamaño de página (opcional, predeterminado: 10)</param>
        [HttpGet("reviews")]
        public async Task<ActionResult<SearchResultDto<ReviewSearchResultDto>>> SearchReviews(
            [FromQuery] string query,
            [FromQuery] int? minRating = null,
            [FromQuery] int? maxRating = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) && !minRating.HasValue && !maxRating.HasValue)
                {
                    return BadRequest(new { message = "Debe proporcionar al menos un criterio de búsqueda" });
                }

                if (minRating.HasValue && (minRating < 1 || minRating > 5))
                {
                    return BadRequest(new { message = "La puntuación mínima debe estar entre 1 y 5" });
                }

                if (maxRating.HasValue && (maxRating < 1 || maxRating > 5))
                {
                    return BadRequest(new { message = "La puntuación máxima debe estar entre 1 y 5" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var result = await _searchService.SearchReviewsAsync(query, minRating, maxRating, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar reseñas");
                return StatusCode(500, new { message = "Ocurrió un error al realizar la búsqueda" });
            }
        }
    }
}