using System;

namespace Core.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<string>> GetAllCategoriesAsync();
    Task<IReadOnlyList<string>> GetSubCategoriesAsync(string category);
    Task<IReadOnlyList<string>> GetArticleTypesAsync(string? category = null);
    Task<Dictionary<string, int>> GetCategoryCountsAsync();
}
