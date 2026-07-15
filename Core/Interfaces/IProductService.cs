using System;
using Core.Entities;
using Core.Specifications;

namespace Core.Interfaces;

public interface IProductService
{
    Task<Product?> GetProductByIdAsync(int id);
    Task<(IReadOnlyList<Product> Products, int TotalCount)> GetProductsAsync(ProductSpecParams specParams);
    Task<Product> CreateProductAsync(Product product);
    Task<bool> UpdateProductAsync(Product product);
    Task<bool> DeleteProductAsync(int id);
    Task<IReadOnlyList<string>> GetBrandsAsync();
    Task<IReadOnlyList<string>> GetCategoriesAsync();
    Task<IReadOnlyList<string>> GetArticleTypesAsync();
    Task<IReadOnlyList<string>> GetColorsAsync();

    Task<IReadOnlyList<string>> GetGendersAsync();
    Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int count = 8);
    Task<IReadOnlyList<Product>> GetNewArrivalsAsync(int count = 8);
    Task<IReadOnlyList<Product>> GetSimilarProductsAsync(int productId, int count = 6);
    Task<IReadOnlyList<Product>> GetTrendingProductsAsync(int count = 8);
}
