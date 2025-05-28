namespace BooksAPIReviews.Models.DTOs
{
    public class BookResponseDto
    {
        public Guid Book_Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string CoverImageUrl { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string Category { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Guid User_Id { get; set; }

        public Guid Review_Id { get; set; }
    }
}
