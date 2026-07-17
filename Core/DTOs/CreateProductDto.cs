using System;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateProductDto
{
    [Required] [MaxLength(500)] public string Name { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;

    [Required] [MaxLength(200)] public string Brand { get; set; } = string.Empty;
    [Required] [MaxLength(100)] public string Category { get; set; } = string.Empty;
    [MaxLength(100)] public string SubCategory { get; set; } = string.Empty;
    [Required] [MaxLength(100)] public string ArticleType { get; set; } = string.Empty;
    [MaxLength(50)] public string Gender { get; set; } = string.Empty;
    [MaxLength(50)] public string AgeGroup { get; set; } = string.Empty;
    [MaxLength(50)] public string BaseColor { get; set; } = string.Empty;
    [MaxLength(50)] public string Season { get; set; } = string.Empty;
    [MaxLength(100)] public string Usage { get; set; } = string.Empty;
    [MaxLength(200)] public string Material { get; set; } = string.Empty;
    [MaxLength(100)] public string Pattern { get; set; } = string.Empty;
    [MaxLength(100)] public string Fit { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    [Required] public decimal Price { get; set; }

    public decimal DiscountPercentage { get; set; }
    [Required] [MaxLength(500)] public string ImageUrl { get; set; } = string.Empty;
    [MaxLength(1000)] public string Tags { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }

    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }
}
