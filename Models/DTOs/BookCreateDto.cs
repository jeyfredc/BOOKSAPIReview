using System.ComponentModel.DataAnnotations;

namespace BooksAPIReviews.Models.DTOs
{
    public class BookCreateDto
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Title { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Author { get; set; }

        public string Description { get; set; }
        public string CoverImageUrl { get; set; }
        public DateTime? PublishedDate { get; set; }

        [StringLength(100)]
        public string Category { get; set; }
    }
}
