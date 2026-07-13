using System;
using Core.Entities;

namespace Core.Specifications;

public class FeaturedProductsSpecification : BaseSpecification<Product>
{
    public FeaturedProductsSpecification(int count)
        : base(x => x.IsFeatured)
    {
        AddOrderByDescending(x => x.Rating);
        ApplyPaging(0, count);
    }
}
