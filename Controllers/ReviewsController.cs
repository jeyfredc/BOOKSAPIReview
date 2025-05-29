using BooksAPIReviews.Models.DTOs;
using BooksAPIReviews.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
        {
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }



        /// <summary>
        /// Obtiene una reseña por su ID
        /// </summary>
        /// <param name="id">ID de la reseña</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReviewResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReviewResponseDto>> GetReview(Guid id)
        {
            try
            {
                var review = await _reviewService.GetReviewByIdAsync(id);

                if (review == null)
                {
                    return NotFound(new { message = $"No se encontró la reseña con ID: {id}" });
                }

                return Ok(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener la reseña con ID: {id}");
                return StatusCode(500, new { message = "Ocurrió un error al procesar la solicitud" });
            }
        }


        /// <summary>
        /// Crea una nueva reseña
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ReviewResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ReviewResponseDto>> CreateReview([FromBody] ReviewCreateDto reviewDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Datos de entrada no válidos", errors = ModelState });
            }

            try
            {
                var createdReview = await _reviewService.CreateReviewAsync(reviewDto);
                return CreatedAtAction(nameof(GetReview), new { id = createdReview.Id }, createdReview);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la reseña");
                return StatusCode(500, new { message = "Ocurrió un error al crear la reseña" });
            }
        }

    }
}
