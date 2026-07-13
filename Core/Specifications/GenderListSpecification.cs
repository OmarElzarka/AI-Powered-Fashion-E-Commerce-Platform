using System;
using Core.Entities;

namespace Core.Specifications;

public class GenderListSpecification : BaseSpecification<Product, string>
{
    public GenderListSpecification()
    {
        AddSelect(x => x.Gender);
        ApplyDistinct();
    }
}
