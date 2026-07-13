using System;
using Core.Entities;

namespace Core.Specifications;

public class SeasonListSpecification : BaseSpecification<Product, string>
{
    public SeasonListSpecification()
    {
        AddSelect(x => x.Season);
        ApplyDistinct();
    }
}
