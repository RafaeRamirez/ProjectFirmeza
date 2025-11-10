using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Contracts.Dtos.Products;

public class ProductQueryParameters
{
    public string? Search { get; set; }
    public bool? OnlyAvailable { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    private const int MaxPageSize = 100;

    private int _pageSize = 20;
    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    private int _page = 1;
    [Range(1, 100000)]
    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}
