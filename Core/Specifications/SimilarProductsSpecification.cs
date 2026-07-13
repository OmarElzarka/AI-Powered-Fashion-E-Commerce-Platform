using System;
using Core.Entities;

namespace Core.Specifications;

public class SimilarProductsSpecification : BaseSpecification<Product>
{
    public SimilarProductsSpecification(Product product, int count)
        : base(x => x.Id != product.Id &&
            (x.Category == product.Category || x.SubCategory == product.SubCategory) &&
            (x.Gender == product.Gender || string.IsNullOrEmpty(product.Gender)))
    {
        AddOrderByDescending(x => x.Rating);
        ApplyPaging(0, count);
    }
}
