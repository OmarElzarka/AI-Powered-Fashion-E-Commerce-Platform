using API.DTOs;
using API.Extensions;
using API.RequestHelpers;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class ProductsController(IProductService productService) : BaseApiController
{
    [Cached(100000)]
    [HttpGet]
    public async Task<ActionResult> GetProducts([FromQuery] ProductSpecParams specParams)
    {
        var (products, count) = await productService.GetProductsAsync(specParams);
        var dtoItems = products.Select(p => p.ToDto()).ToList();
        var pagination = new Pagination<ProductDto>(specParams.PageIndex, specParams.PageSize, count, dtoItems);
        return Ok(pagination);
    }

    [Cached(100000)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product.ToDto());
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
    {
        var product = dto.ToEntity();
        var created = await productService.CreateProductAsync(product);
        return CreatedAtAction("GetProduct", new { id = created.Id }, created.ToDto());
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, CreateProductDto dto)
    {
        var existing = await productService.GetProductByIdAsync(id);
        if (existing == null) return NotFound();

        // Map dto fields onto existing entity
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Brand = dto.Brand;
        existing.Category = dto.Category;
        existing.SubCategory = dto.SubCategory;
        existing.ArticleType = dto.ArticleType;
        existing.Gender = dto.Gender;
        existing.BaseColor = dto.BaseColor;
        existing.Season = dto.Season;
        existing.Usage = dto.Usage;
        existing.Material = dto.Material;
        existing.Pattern = dto.Pattern;
        existing.Price = dto.Price;
        existing.DiscountPercentage = dto.DiscountPercentage;
        existing.ImageUrl = dto.ImageUrl;
        existing.Tags = dto.Tags;
        existing.IsFeatured = dto.IsFeatured;
        existing.IsNewArrival = dto.IsNewArrival;
        existing.QuantityInStock = dto.QuantityInStock;

        var success = await productService.UpdateProductAsync(existing);
        if (success) return NoContent();
        return BadRequest("Problem updating product");
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var success = await productService.DeleteProductAsync(id);
        if (success) return NoContent();
        return NotFound();
    }

    [Cached(100000)]
    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    {
        return Ok(await productService.GetBrandsAsync());
    }

    [Cached(100000)]
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories()
    {
        return Ok(await productService.GetCategoriesAsync());
    }

    [Cached(100000)]
    [HttpGet("article-types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetArticleTypes()
    {
        return Ok(await productService.GetArticleTypesAsync());
    }

    [Cached(100000)]
    [HttpGet("colors")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetColors()
    {
        return Ok(await productService.GetColorsAsync());
    }

    [Cached(100000)]
    [HttpGet("seasons")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetSeasons()
    {
        return Ok(await productService.GetSeasonsAsync());
    }

    [Cached(100000)]
    [HttpGet("genders")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetGenders()
    {
        return Ok(await productService.GetGendersAsync());
    }

    [Cached(100000)]
    [HttpGet("featured")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetFeatured([FromQuery] int count = 8)
    {
        var products = await productService.GetFeaturedProductsAsync(count);
        return Ok(products.Select(p => p.ToDto()).ToList());
    }

    [Cached(100000)]
    [HttpGet("new-arrivals")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetNewArrivals([FromQuery] int count = 8)
    {
        var products = await productService.GetNewArrivalsAsync(count);
        return Ok(products.Select(p => p.ToDto()).ToList());
    }

    [Cached(100000)]
    [HttpGet("trending")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetTrending([FromQuery] int count = 8)
    {
        var products = await productService.GetTrendingProductsAsync(count);
        return Ok(products.Select(p => p.ToDto()).ToList());
    }

    [Cached(100000)]
    [HttpGet("{id:int}/similar")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetSimilar(int id, [FromQuery] int count = 6)
    {
        var products = await productService.GetSimilarProductsAsync(id, count);
        return Ok(products.Select(p => p.ToDto()).ToList());
    }
}
