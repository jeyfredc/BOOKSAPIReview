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
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly ILogger<BooksController> _logger;

        public BooksController(IBookService bookService, ILogger<BooksController> logger)
        {
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene todos los libros
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<BookResponseDto>))]
        public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetBooks()
        {
            try
            {
                var books = await _bookService.GetBooksAsync();
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los libros");
                return StatusCode(500, new { message = "Ocurrió un error al procesar la solicitud" });
            }
        }

        /// <summary>
        /// Obtiene un libro por su ID
        /// </summary>
        /// <param name="id">ID del libro</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BookResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookResponseDto>> GetBook(Guid id)
        {
            try
            {
                var book = await _bookService.GetBookByIdAsync(id);

                if (book == null)
                {
                    return NotFound(new { message = $"No se encontró el libro con ID: {id}" });
                }

                return Ok(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el libro con ID: {id}");
                return StatusCode(500, new { message = "Ocurrió un error al procesar la solicitud" });
            }
        }

        /// <summary>
        /// Crea un nuevo libro
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(BookResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BookResponseDto>> CreateBook([FromBody] BookCreateDto bookDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Datos de entrada no válidos", errors = ModelState });
            }

            try
            {
                var createdBook = await _bookService.CreateBookAsync(bookDto);
                return CreatedAtAction(nameof(GetBook), new { id = createdBook.Book_Id }, createdBook);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el libro");
                return StatusCode(500, new { message = "Ocurrió un error al crear el libro" });
            }
        }

        /// <summary>
        /// Actualiza un libro existente
        /// </summary>
        /// <param name="id">ID del libro a actualizar</param>
        /// <param name="bookDto">Datos actualizados del libro</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBook(Guid id, [FromBody] BookCreateDto bookDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Datos de entrada no válidos", errors = ModelState });
            }

            try
            {
                var updated = await _bookService.UpdateBookAsync(id, bookDto);

                if (!updated)
                {
                    return NotFound(new { message = $"No se encontró el libro con ID: {id}" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el libro con ID: {id}");
                return StatusCode(500, new { message = "Ocurrió un error al actualizar el libro" });
            }
        }

        /// <summary>
        /// Elimina un libro
        /// </summary>
        /// <param name="id">ID del libro a eliminar</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBook(Guid id)
        {
            try
            {
                var deleted = await _bookService.DeleteBookAsync(id);

                if (!deleted)
                {
                    return NotFound(new { message = $"No se encontró el libro con ID: {id}" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el libro con ID: {id}");
                return StatusCode(500, new { message = "Ocurrió un error al eliminar el libro" });
            }
        }
    }
}