using BooksAPIReviews.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public interface IReviewService
    {
        Task<List<ReviewResponseDto>> GetReviewByIdAsync(Guid id);
        Task<IEnumerable<ReviewResponseDto>> GetReviewsByBookIdAsync(Guid bookId);
        Task<ReviewResponseDto> CreateReviewAsync(ReviewCreateDto reviewDto);
        Task<bool> ReviewExistsAsync(Guid id);
    }
}