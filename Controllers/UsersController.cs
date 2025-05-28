using BooksAPIReviews.Interfaces;
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
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene todos los usuarios
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        {
            try
            {
                var users = await _userService.GetUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los usuarios");
                return StatusCode(500, new { message = "Ocurrió un error al procesar la solicitud" });
            }
        }

        /// <summary>
        /// Obtiene un usuario por su ID
        /// </summary>
        /// <param name="id">ID del usuario</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = $"No se encontró el usuario con ID: {id}" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el usuario con ID: {id}");
                return StatusCode(500, new { message = "Ocurrió un error al procesar la solicitud" });
            }
        }


        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        /// <param name="id">ID del usuario a actualizar</param>
        /// <param name="userDto">Datos actualizados del usuario</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserCreateDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Datos de entrada no válidos", errors = ModelState });
            }

            try
            {
                var userExists = await _userService.UserExistsAsync(id);
                if (!userExists)
                {
                    return NotFound(new { message = $"No se encontró el usuario con ID: {id}" });
                }

                var updated = await _userService.UpdateUserAsync(id, userDto);

                if (!updated)
                {
                    return StatusCode(500, new { message = "No se pudo actualizar el usuario" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el usuario con ID: {id}");
                return StatusCode(500, new { message = "Ocurrió un error al actualizar el usuario" });
            }
        }

        /// <summary>
        /// Elimina un usuario
        /// </summary>
        /// <param name="id">ID del usuario a eliminar</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var userExists = await _userService.UserExistsAsync(id);
                if (!userExists)
                {
                    return NotFound(new { message = $"No se encontró el usuario con ID: {id}" });
                }

                var deleted = await _userService.DeleteUserAsync(id);

                if (!deleted)
                {
                    return StatusCode(500, new { message = "No se pudo eliminar el usuario" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el usuario con ID: {id}");
                return StatusCode(500, new { message = "Ocurrió un error al eliminar el usuario" });
            }
        }
    }
}