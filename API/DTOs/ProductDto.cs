using System;

namespace API.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string ArticleType { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public string BaseColor { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Fit { get; set; } = string.Empty;
    public string StyleType { get; set; } = string.Empty;
    public string FashionType { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountedPrice => DiscountPercentage > 0
        ? Math.Round(Price * (1 - DiscountPercentage / 100), 2)
        : Price;
    public double Rating { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string FrontImageUrl { get; set; } = string.Empty;
    public string BackImageUrl { get; set; } = string.Empty;
    public string SearchImageUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }
    public int QuantityInStock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
