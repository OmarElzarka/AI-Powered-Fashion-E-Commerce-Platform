using System;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CategoryService(StoreContext context) : ICategoryService
{
    public async Task<IReadOnlyList<string>> GetAllCategoriesAsync()
    {
        return await context.Products
            .Select(p => p.Category)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetSubCategoriesAsync(string category)
    {
        return await context.Products
            .Where(p => p.Category == category && !string.IsNullOrEmpty(p.SubCategory))
            .Select(p => p.SubCategory)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetArticleTypesAsync(string? category = null)
    {
        var query = context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category == category);
        }

        return await query
            .Select(p => p.ArticleType)
            .Where(a => !string.IsNullOrEmpty(a))
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetCategoryCountsAsync()
    {
        return await context.Products
            .Where(p => !string.IsNullOrEmpty(p.Category))
            .GroupBy(p => p.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count);
    }
}
