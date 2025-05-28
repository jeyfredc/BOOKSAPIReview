using System.ComponentModel.DataAnnotations;

namespace BooksAPIReviews.Models.DTOs
{
    public class CategoryCreateDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
    }
}