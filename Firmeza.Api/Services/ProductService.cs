using AutoMapper;
using AutoMapper.QueryableExtensions;
using Firmeza.Api.Contracts.Dtos.Products;
using Firmeza.Api.Contracts.Responses;
using Firmeza.Api.Data;
using Firmeza.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Services;

public class ProductService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public ProductService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<PagedResponse<ProductDto>> SearchAsync(ProductQueryParameters parameters)
    {
        var query = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var trimmed = parameters.Search.Trim();
            if (_db.Database.IsNpgsql())
            {
                var term = $"%{trimmed}%";
                query = query.Where(p => EF.Functions.ILike(p.Name, term));
            }
            else
            {
                var lowered = trimmed.ToLowerInvariant();
                query = query.Where(p => p.Name.ToLower().Contains(lowered));
            }
        }

        if (parameters.OnlyAvailable == true)
        {
            query = query.Where(p => p.IsActive && p.Stock > 0);
        }

        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(p => p.UnitPrice >= parameters.MinPrice.Value);
        }

        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(p => p.UnitPrice <= parameters.MaxPrice.Value);
        }

        query = parameters.SortBy?.ToLowerInvariant() switch
        {
            "price" => parameters.SortDesc ? query.OrderByDescending(p => p.UnitPrice) : query.OrderBy(p => p.UnitPrice),
            "stock" => parameters.SortDesc ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock),
            _ => parameters.SortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };

        var total = await query.LongCountAsync();
        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResponse<ProductDto>(items, parameters.Page, parameters.PageSize, total);
    }

    public Task<ProductDto?> GetByIdAsync(Guid id) =>
        _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

    public async Task<ProductDto> CreateAsync(ProductCreateDto dto, string userId)
    {
        var product = _mapper.Map<Product>(dto);
        product.Name = dto.Name.Trim();
        product.CreatedByUserId = userId;

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<bool> UpdateAsync(ProductUpdateDto dto)
    {
        var existing = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.Id);
        if (existing is null)
        {
            return false;
        }

        existing.Name = dto.Name.Trim();
        existing.UnitPrice = dto.UnitPrice;
        existing.Stock = dto.Stock;
        existing.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ProductDeleteResultDto> DeleteAsync(Guid id, bool force = false)
    {
        var result = new ProductDeleteResultDto();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return result;
        }

        var saleItems = await _db.SaleItems.Include(i => i.Sale)
            .Where(i => i.ProductId == id)
            .ToListAsync();

        result.HasSales = saleItems.Count > 0;

        if (result.HasSales && !force)
        {
            product.IsActive = false;
            await _db.SaveChangesAsync();
            result.SetInactive = true;
            return result;
        }

        if (result.HasSales)
        {
            var saleIds = saleItems.Select(i => i.SaleId).Distinct().ToList();
            var sales = await _db.Sales.Include(s => s.Items)
                .Where(s => saleIds.Contains(s.Id))
                .ToListAsync();

            _db.SaleItems.RemoveRange(saleItems);
            foreach (var sale in sales)
            {
                var remaining = sale.Items.Where(i => i.ProductId != id).ToList();
                sale.Total = remaining.Sum(i => i.Subtotal);
                if (remaining.Count == 0)
                {
                    _db.Sales.Remove(sale);
                    result.DeletedSales.Add(sale.Id);
                }
            }
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        result.Removed = true;
        return result;
    }
}
