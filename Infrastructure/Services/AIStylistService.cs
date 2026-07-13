using System;
using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Placeholder AI Stylist service. Future implementation will use LLM-powered
/// outfit generation, color theory algorithms, and occasion-based styling.
/// </summary>
public class AIStylistService : IAIStylistService
{
    public Task<IReadOnlyList<Product>> GetOutfitSuggestionsAsync(int productId, int count = 4)
    {
        return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }

    public Task<IReadOnlyList<Product>> GetStyleByOccasionAsync(string occasion, string? gender = null, int count = 8)
    {
        return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }

    public Task<IReadOnlyList<Product>> GetSeasonalPicksAsync(string season, string? gender = null, int count = 8)
    {
        return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }

    public Task<IReadOnlyList<string>> GetColorPaletteSuggestionsAsync(string baseColor)
    {
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
