using System;
using API.DTOs;
using Core.Entities;

namespace API.Extensions;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Brand = product.Brand,
            Category = product.Category,
            SubCategory = product.SubCategory,
            ArticleType = product.ArticleType,
            Gender = product.Gender,
            AgeGroup = product.AgeGroup,
            BaseColor = product.BaseColor,
            Season = product.Season,
            Usage = product.Usage,
            Material = product.Material,
            Pattern = product.Pattern,
            Fit = product.Fit,
            StyleType = product.StyleType,
            FashionType = product.FashionType,
            Year = product.Year,
            Price = product.Price,
            DiscountPercentage = product.DiscountPercentage,
            Rating = product.Rating,
            ImageUrl = product.ImageUrl,
            FrontImageUrl = product.FrontImageUrl,
            BackImageUrl = product.BackImageUrl,
            SearchImageUrl = product.SearchImageUrl,
            Tags = string.IsNullOrEmpty(product.Tags)
                ? []
                : product.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            IsFeatured = product.IsFeatured,
            IsNewArrival = product.IsNewArrival,
            QuantityInStock = product.QuantityInStock,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public static Product ToEntity(this CreateProductDto dto)
    {
        return new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Brand = dto.Brand,
            Category = dto.Category,
            SubCategory = dto.SubCategory,
            ArticleType = dto.ArticleType,
            Gender = dto.Gender,
            AgeGroup = dto.AgeGroup,
            BaseColor = dto.BaseColor,
            Season = dto.Season,
            Usage = dto.Usage,
            Material = dto.Material,
            Pattern = dto.Pattern,
            Fit = dto.Fit,
            Price = dto.Price,
            DiscountPercentage = dto.DiscountPercentage,
            ImageUrl = dto.ImageUrl,
            Tags = dto.Tags,
            IsFeatured = dto.IsFeatured,
            IsNewArrival = dto.IsNewArrival,
            QuantityInStock = dto.QuantityInStock
        };
    }
}
