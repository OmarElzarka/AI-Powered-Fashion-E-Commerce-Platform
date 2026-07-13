using System;
using Core.Entities;

namespace Core.Interfaces;

/// <summary>
/// AI Recommendation service interface. Future-ready for ML/LLM-powered recommendations.
/// Current implementation uses rule-based matching (category, color, gender).
/// </summary>
public interface IAIRecommendationService
{
    Task<IReadOnlyList<Product>> GetSimilarProductsAsync(int productId, int count = 6);
    Task<IReadOnlyList<Product>> GetRecommendedProductsAsync(string? userId = null, int count = 8);
    Task<IReadOnlyList<Product>> GetPersonalizedFeedAsync(string userId, int count = 20);
}
