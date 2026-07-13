using System;
using Core.Entities;

namespace Core.Specifications;

public class NewArrivalsSpecification : BaseSpecification<Product>
{
    public NewArrivalsSpecification(int count)
        : base(x => x.IsNewArrival)
    {
        AddOrderByDescending(x => x.CreatedAt);
        ApplyPaging(0, count);
    }
}
