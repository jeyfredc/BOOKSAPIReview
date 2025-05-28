using BooksAPIReviews.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooksAPIReviews.Services
{
    public interface IReviewService
    {
        Task<IEnumerable<ReviewResponseDto>> GetReviewsAsync();
        Task<ReviewResponseDto> GetReviewByIdAsync(Guid id);
        Task<IEnumerable<ReviewResponseDto>> GetReviewsByBookIdAsync(Guid bookId);
        Task<IEnumerable<ReviewResponseDto>> GetReviewsByUserIdAsync(Guid userId);
        Task<ReviewResponseDto> CreateReviewAsync(ReviewCreateDto reviewDto);
        Task<bool> UpdateReviewAsync(Guid id, ReviewCreateDto reviewDto);
        Task<bool> DeleteReviewAsync(Guid id);
        Task<bool> ReviewExistsAsync(Guid id);
    }
}