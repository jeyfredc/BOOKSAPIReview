using System.ComponentModel.DataAnnotations;

namespace BooksAPIReviews.Models.DTOs
{
    public class UserLoginDto
    {
        [Required]
        public string UsernameOrEmail { get; set; }

        [Required]
        public string Password { get; set; }
    }
}