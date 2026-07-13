using System;
using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Placeholder AI Shopping Agent service. Future implementation will use
/// LLM-powered conversational commerce, natural language search, and
/// autonomous cart management.
/// </summary>
public class AIShoppingAgentService : IAIShoppingAgentService
{
    public Task<IReadOnlyList<Product>> SearchByNaturalLanguageAsync(string query, int count = 10)
    {
        return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }

    public Task<string> GetProductComparisonAsync(int productId1, int productId2)
    {
        return Task.FromResult("AI comparison feature coming soon.");
    }

    public Task<string> GetProductExplanationAsync(int productId)
    {
        return Task.FromResult("AI product explanation feature coming soon.");
    }
}
