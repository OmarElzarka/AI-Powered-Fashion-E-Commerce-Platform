using System;
using Core.Entities;

namespace Core.Interfaces;

/// <summary>
/// AI Stylist service interface. Future-ready for outfit recommendation,
/// color matching, occasion-based styling, and season-aware suggestions.
/// </summary>
public interface IAIStylistService
{
    Task<IReadOnlyList<Product>> GetOutfitSuggestionsAsync(int productId, int count = 4);
    Task<IReadOnlyList<Product>> GetStyleByOccasionAsync(string occasion, string? gender = null, int count = 8);
    Task<IReadOnlyList<Product>> GetSeasonalPicksAsync(string season, string? gender = null, int count = 8);
    Task<IReadOnlyList<string>> GetColorPaletteSuggestionsAsync(string baseColor);
}
