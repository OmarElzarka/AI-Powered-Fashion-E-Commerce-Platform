using System;
using Core.Entities;

namespace Core.Specifications;

public class ProductSpecification : BaseSpecification<Product>
{
    public ProductSpecification(ProductSpecParams p)
        : base(x =>
            (string.IsNullOrEmpty(p.Search)
                || x.Brand.ToLower().Contains(p.Search)
                || x.Name.ToLower().Contains(p.Search) 
                || x.Tags.ToLower().Contains(p.Search)) &&
            (!p.Brands.Any() || p.Brands.Contains(x.Brand)) &&
            (!p.Categories.Any() || p.Categories.Contains(x.Category)) &&
            (!p.SubCategories.Any() || p.SubCategories.Contains(x.SubCategory)) &&
            (!p.Types.Any() || p.Types.Contains(x.ArticleType)) &&
            (!p.Genders.Any() || p.Genders.Contains(x.Gender)) &&
            (!p.Colors.Any() || p.Colors.Contains(x.BaseColor)) &&
            (!p.Necks.Any() || p.Necks.Contains(x.Neck)) &&
            (!p.Sleeves.Any() || p.Sleeves.Contains(x.Sleeve)) &&
            (!p.PriceMin.HasValue || x.Price >= p.PriceMin.Value) &&
            (!p.PriceMax.HasValue || x.Price <= p.PriceMax.Value) &&
            (!p.MinRating.HasValue || x.Rating >= p.MinRating.Value) &&
            (!p.MinDiscount.HasValue || x.DiscountPercentage >= p.MinDiscount.Value) &&
            (!p.IsFeatured.HasValue || x.IsFeatured == p.IsFeatured.Value) &&
            (!p.IsNewArrival.HasValue || x.IsNewArrival == p.IsNewArrival.Value))
    {
        ApplyPaging(p.PageSize * (p.PageIndex - 1), p.PageSize);

        switch (p.Sort)
        {
            case "priceAsc":
                AddOrderBy(x => x.Price);
                break;
            case "priceDesc":
                AddOrderByDescending(x => x.Price);
                break;
            case "newest":
                AddOrderByDescending(x => x.CreatedAt);
                break;
            case "rating":
                AddOrderByDescending(x => x.Rating);
                break;
            case "popularity":
                AddOrderByDescending(x => x.Rating);
                break;
            default:
                AddOrderByDescending(x => x.CreatedAt);
                break;
        }
    }
}
