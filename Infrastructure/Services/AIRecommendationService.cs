using System;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Infrastructure.Services;

/// <summary>
/// Rule-based recommendation service. Future implementation will use ML models,
/// embeddings, and collaborative filtering for smarter recommendations.
/// </summary>
public class AIRecommendationService(IUnitOfWork unit) : IAIRecommendationService
{
    public async Task<IReadOnlyList<Product>> GetSimilarProductsAsync(int productId, int count = 6)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(productId);
        if (product == null) return Array.Empty<Product>();

        var spec = new SimilarProductsSpecification(product, count);
        return await unit.Repository<Product>().ListAsync(spec);
    }

    public async Task<IReadOnlyList<Product>> GetRecommendedProductsAsync(string? userId = null, int count = 8)
    {
        // Future: use user history and collaborative filtering
        var spec = new TrendingProductsSpecification(count);
        return await unit.Repository<Product>().ListAsync(spec);
    }

    public async Task<IReadOnlyList<Product>> GetPersonalizedFeedAsync(string userId, int count = 20)
    {
        // Future: use user embeddings and product embeddings for similarity search
        var spec = new TrendingProductsSpecification(count);
        return await unit.Repository<Product>().ListAsync(spec);
    }
}
