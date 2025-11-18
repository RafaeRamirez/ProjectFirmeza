using AutoMapper;
using AutoMapper.QueryableExtensions;
using Firmeza.Api.Contracts.Dtos.ProductRequests;
using Firmeza.Api.Data;
using Firmeza.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Services;

public class ProductRequestService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public ProductRequestService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public Task<List<ProductRequestDto>> ListByUserAsync(string userId) =>
        _db.ProductRequests
            .AsNoTracking()
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ProjectTo<ProductRequestDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<ProductRequestBatchResultDto> CreateBatchAsync(string userId, string? userEmail, IEnumerable<ProductRequestCreateItemDto> items)
    {
        var result = new ProductRequestBatchResultDto();
        var normalizedEmail = userEmail ?? string.Empty;
        var sanitizedItems = items?
            .Where(i => i.ProductId != Guid.Empty && i.Quantity > 0)
            .ToList() ?? new List<ProductRequestCreateItemDto>();

        if (sanitizedItems.Count == 0)
        {
            return result;
        }

        var productIds = sanitizedItems.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var requestsToInsert = new List<ProductRequest>();

        foreach (var item in sanitizedItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                result.Errors.Add(new ProductRequestErrorDto
                {
                    ProductId = item.ProductId,
                    Message = "Producto inexistente."
                });
                continue;
            }

            if (!product.IsActive || product.Stock <= 0)
            {
                result.Errors.Add(new ProductRequestErrorDto
                {
                    ProductId = item.ProductId,
                    Message = "Producto sin stock disponible."
                });
                continue;
            }

            var request = new ProductRequest
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                Note = item.Note,
                RequestedByUserId = userId,
                RequestedByEmail = normalizedEmail
            };
            requestsToInsert.Add(request);
        }

        if (requestsToInsert.Count > 0)
        {
            await _db.ProductRequests.AddRangeAsync(requestsToInsert);
            await _db.SaveChangesAsync();

            var createdIds = requestsToInsert.Select(r => r.Id).ToList();
            result.Requests = await _db.ProductRequests
                .AsNoTracking()
                .Where(r => createdIds.Contains(r.Id))
                .OrderByDescending(r => r.RequestedAt)
                .ProjectTo<ProductRequestDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        return result;
    }
}
