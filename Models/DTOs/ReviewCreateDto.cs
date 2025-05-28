using System;
using System.ComponentModel.DataAnnotations;

namespace BooksAPIReviews.Models.DTOs
{
    public class ReviewCreateDto
    {
        [Required]
        public Guid BookId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public int Rating { get; set; }

        [StringLength(2000, ErrorMessage = "El comentario no debe exceder los 2000 caracteres")]
        public string Comment { get; set; }

        [Required]
        public Guid UserId { get; set; }
    }
}