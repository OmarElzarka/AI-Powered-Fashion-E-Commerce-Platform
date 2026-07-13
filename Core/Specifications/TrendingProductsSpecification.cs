using System;
using Core.Entities;

namespace Core.Specifications;

public class TrendingProductsSpecification : BaseSpecification<Product>
{
    public TrendingProductsSpecification(int count)
        : base(x => x.Rating >= 3.5)
    {
        AddOrderByDescending(x => x.Rating);
        ApplyPaging(0, count);
    }
}
