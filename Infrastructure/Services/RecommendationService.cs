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

public class RecommendationService : IRecommendationService, IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecommendationService> _logger;
    private readonly ConcurrentDictionary<int, float[]> _productVectors = new();

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
        
        var embeddings = await context.ProductEmbeddings.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var embedding in embeddings)
        {
            if (!string.IsNullOrEmpty(embedding.VectorJson))
            {
                var vector = JsonSerializer.Deserialize<float[]>(embedding.VectorJson);
                if (vector != null)
                {
                    _productVectors.TryAdd(embedding.ProductId, vector);
                }
            }
        }
        
        _logger.LogInformation("Loaded {Count} embeddings into cache.", _productVectors.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<List<Product>> GetRecommendationsAsync(int productId, int limit = 5)
    {
        if (!_productVectors.TryGetValue(productId, out var targetVector))
        {
            return new List<Product>();
        }

        var similarities = new List<(int ProductId, float Similarity)>();
        
        foreach (var kvp in _productVectors)
        {
            if (kvp.Key == productId) continue;
            
            float sim = CosineSimilarity(targetVector, kvp.Value);
            similarities.Add((kvp.Key, sim));
        }
        
        var topIds = similarities
            .OrderByDescending(x => x.Similarity)
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
