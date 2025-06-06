﻿// En Models/DTOs/SearchDto.cs
namespace BooksAPIReviews.Models.DTOs
{
    public class SearchResultDto<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class BookSearchResultDto
    {
        public Guid Book_Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal? AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string? CoverImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }


}