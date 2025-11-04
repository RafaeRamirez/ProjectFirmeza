using Firmeza.WebApplication.Data;
using Firmeza.WebApplication.Interfaces;
using Firmeza.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.WebApplication.Repositories;

/// <summary>
/// EF Core repository for Product entity.
/// </summary>
public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<Product>> ListAsync(string? search, CancellationToken ct = default)
    {
        var q = _db.Products.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(p => p.Name.ToLower().Contains(term));
        }
        return await q.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return;
        _db.Products.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
