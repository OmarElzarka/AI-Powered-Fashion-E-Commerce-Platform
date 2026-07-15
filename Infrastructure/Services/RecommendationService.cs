using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ProductCacheItem
{
    public int Id { get; set; }
    public float[] Vector { get; set; } = [];
    public string Gender { get; set; } = string.Empty;
    public string ArticleType { get; set; } = string.Empty;
    public string BaseColor { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
}

public class RecommendationService : IRecommendationService, IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecommendationService> _logger;
    private readonly ConcurrentDictionary<int, ProductCacheItem> _productCache = new();

    public RecommendationService(IServiceScopeFactory scopeFactory, ILogger<RecommendationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading product embeddings into memory cache...");
        
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreContext>();
        
        var embeddings = await context.ProductEmbeddings
            .Include(e => e.Product)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
            
        foreach (var embedding in embeddings)
        {
            if (!string.IsNullOrEmpty(embedding.VectorJson) && embedding.Product != null)
            {
                var vector = JsonSerializer.Deserialize<float[]>(embedding.VectorJson);
                if (vector != null)
                {
                    _productCache.TryAdd(embedding.ProductId, new ProductCacheItem
                    {
                        Id = embedding.ProductId,
                        Vector = vector,
                        Gender = embedding.Product.Gender ?? "",
                        ArticleType = embedding.Product.ArticleType ?? "",
                        BaseColor = embedding.Product.BaseColor ?? "",
                        Brand = embedding.Product.Brand ?? ""
                    });
                }
            }
        }
        
        _logger.LogInformation("Loaded {Count} items into recommendation cache.", _productCache.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<List<Product>> GetRecommendationsAsync(int productId, int limit = 5)
    {
        if (!_productCache.TryGetValue(productId, out var targetItem))
        {
            return new List<Product>();
        }

        var similarities = new List<(int ProductId, float Score)>();
        
        foreach (var kvp in _productCache)
        {
            if (kvp.Key == productId) continue;
            
            var candidate = kvp.Value;

            // HARD FILTER: Gender
            // If target is specific (Men/Women/Boys/Girls), candidate must match exactly or be Unisex
            bool targetIsSpecific = !targetItem.Gender.Equals("Unisex", StringComparison.OrdinalIgnoreCase);
            bool candidateIsSpecific = !candidate.Gender.Equals("Unisex", StringComparison.OrdinalIgnoreCase);
            
            if (targetIsSpecific && candidateIsSpecific && 
                !targetItem.Gender.Equals(candidate.Gender, StringComparison.OrdinalIgnoreCase))
            {
                continue; // Incompatible gender, discard entirely
            }

            // BASE SEMANTIC SCORE
            float score = CosineSimilarity(targetItem.Vector, candidate.Vector);
            
            // HYBRID BOOSTING
            if (targetItem.ArticleType.Equals(candidate.ArticleType, StringComparison.OrdinalIgnoreCase))
                score += 0.15f; // High Priority
                
            if (targetItem.BaseColor.Equals(candidate.BaseColor, StringComparison.OrdinalIgnoreCase))
                score += 0.05f; // Medium Priority
                
            if (targetItem.Brand.Equals(candidate.Brand, StringComparison.OrdinalIgnoreCase))
                score += 0.02f; // Low Priority

            similarities.Add((kvp.Key, score));
        }
        
        var topIds = similarities
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => x.ProductId)
            .ToList();
            
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreContext>();
        
        var products = await context.Products
            .Where(p => topIds.Contains(p.Id))
            .ToListAsync();
            
        // Preserve ordering
        return products.OrderBy(p => topIds.IndexOf(p.Id)).ToList();
    }

    private static float CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length) return 0;
        
        float dotProduct = 0;
        float normA = 0;
        float normB = 0;
        
        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }
        
        if (normA == 0 || normB == 0) return 0;
        
        return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
