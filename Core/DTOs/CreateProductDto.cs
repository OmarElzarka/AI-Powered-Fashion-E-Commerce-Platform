using System;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateProductDto
{
    [Required] public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Required] public string Brand { get; set; } = string.Empty;
    [Required] public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    [Required] public string ArticleType { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public string BaseColor { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Fit { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    [Required] public decimal Price { get; set; }

    public decimal DiscountPercentage { get; set; }
    [Required] public string ImageUrl { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }

    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }
}
