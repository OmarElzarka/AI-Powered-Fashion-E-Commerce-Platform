using System;

namespace Core.Entities;

public class Product : BaseEntity
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Brand { get; set; }
    public required string Category { get; set; }
    public string SubCategory { get; set; } = string.Empty;
    public required string ArticleType { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public string BaseColor { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Fit { get; set; } = string.Empty;
    public string Neck { get; set; } = string.Empty;
    public string Sleeve { get; set; } = string.Empty;
    public string StyleType { get; set; } = string.Empty;
    public string FashionType { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public double Rating { get; set; }
    public required string ImageUrl { get; set; }
    public string FrontImageUrl { get; set; } = string.Empty;
    public string BackImageUrl { get; set; } = string.Empty;
    public string SearchImageUrl { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }
    public int QuantityInStock { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
