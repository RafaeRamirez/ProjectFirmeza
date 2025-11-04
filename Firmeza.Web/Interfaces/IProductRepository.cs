using Firmeza.WebApplication.Models;

namespace Firmeza.WebApplication.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Product>> ListAsync(string? search, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
