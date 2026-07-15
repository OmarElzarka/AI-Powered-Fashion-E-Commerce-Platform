using System;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ProductService(IUnitOfWork unit, StoreContext context) : IProductService
{
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await unit.Repository<Product>().GetByIdAsync(id);
    }

    public async Task<(IReadOnlyList<Product> Products, int TotalCount)> GetProductsAsync(
        ProductSpecParams specParams)
    {
        var spec = new ProductSpecification(specParams);
        var products = await unit.Repository<Product>().ListAsync(spec);
        var count = await unit.Repository<Product>().CountAsync(spec);
        return (products, count);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Product>().Add(product);
        await unit.Complete();
        return product;
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Product>().Update(product);
        return await unit.Complete();
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product == null) return false;

        unit.Repository<Product>().Remove(product);
        return await unit.Complete();
    }

    public async Task<IReadOnlyList<string>> GetBrandsAsync()
    {
        return await context.Products
            .GroupBy(p => p.Brand)
            .Where(g => g.Count() >= 20)
            .Select(g => g.Key)
            .Where(b => !string.IsNullOrEmpty(b))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync()
    {
        var spec = new CategoryListSpecification();
        return await unit.Repository<Product>().ListAsync(spec);
    }

    public async Task<IReadOnlyList<string>> GetArticleTypesAsync()
    {
        var spec = new TypeListSpecification();
        return await unit.Repository<Product>().ListAsync(spec);
    }

    public Task<IReadOnlyList<string>> GetColorsAsync()
    {
        var commonColors = new List<string> { "Black", "White", "Blue", "Red", "Green", "Grey", "Brown", "Pink", "Yellow", "Beige", "Navy Blue" };
        return Task.FromResult<IReadOnlyList<string>>(commonColors);
    }



    public async Task<IReadOnlyList<string>> GetGendersAsync()
    {
        var spec = new GenderListSpecification();
        return await unit.Repository<Product>().ListAsync(spec);
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int count = 8)
    {
        var spec = new FeaturedProductsSpecification(count);
        return await unit.Repository<Product>().ListAsync(spec);
    }

    public async Task<IReadOnlyList<Product>> GetNewArrivalsAsync(int count = 8)
    {
        var spec = new NewArrivalsSpecification(count);
        return await unit.Repository<Product>().ListAsync(spec);
    }

    public async Task<IReadOnlyList<Product>> GetSimilarProductsAsync(int productId, int count = 6)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(productId);
        if (product == null) return Array.Empty<Product>();

        var spec = new SimilarProductsSpecification(product, count);
        return await unit.Repository<Product>().ListAsync(spec);
    }

    public async Task<IReadOnlyList<Product>> GetTrendingProductsAsync(int count = 8)
    {
        var spec = new TrendingProductsSpecification(count);
        return await unit.Repository<Product>().ListAsync(spec);
    }
}
