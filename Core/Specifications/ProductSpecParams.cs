using System;

namespace Core.Specifications;

public class ProductSpecParams : PagingParams
{
    private List<string> _brands = [];
    public List<string> Brands
    {
        get => _brands;
        set
        {
            _brands = value.SelectMany(b => b.Split(',',
                StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    private List<string> _categories = [];
    public List<string> Categories
    {
        get => _categories;
        set
        {
            _categories = value.SelectMany(c => c.Split(',',
                StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    private List<string> _subCategories = [];
    public List<string> SubCategories
    {
        get => _subCategories;
        set
        {
            _subCategories = value.SelectMany(s => s.Split(',',
                StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    private List<string> _genders = [];
    public List<string> Genders
    {
        get => _genders;
        set
        {
            _genders = value.SelectMany(g => g.Split(',',
                StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    private List<string> _colors = [];
    public List<string> Colors
    {
        get => _colors;
        set
        {
            _colors = value.SelectMany(c => c.Split(',',
                StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    private List<string> _seasons = [];
    public List<string> Seasons
    {
        get => _seasons;
        set
        {
            _seasons = value.SelectMany(s => s.Split(',',
                StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    private List<string> _usages = [];
    public List<string> Usages
    {
        get => _usages;
        set
        {
            _usages = value.SelectMany(u => u.Split(',',
                StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    private List<string> _articleTypes = [];
    public List<string> ArticleTypes
    {
        get => _articleTypes;
        set
        {
            _articleTypes = value.SelectMany(a => a.Split(',',
                StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    private List<string> _patterns = [];
    public List<string> Patterns
    {
        get => _patterns;
        set
        {
            _patterns = value.SelectMany(p => p.Split(',',
                StringSplitOptions.RemoveEmptyEntries)).ToList();
        }
    }

    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public double? MinRating { get; set; }
    public decimal? MinDiscount { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? IsNewArrival { get; set; }

    public string? Sort { get; set; }

    private string? _search;
    public string Search
    {
        get => _search ?? "";
        set => _search = value.ToLower();
    }
}
