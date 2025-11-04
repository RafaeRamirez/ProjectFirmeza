using Firmeza.WebApplication.Interfaces;
using Firmeza.WebApplication.Models;

namespace Firmeza.WebApplication.Services;

/// <summary>
/// Business logic for Products (keeps controllers thin).
/// </summary>
public class ProductService
{
    private readonly IProductRepository _repo;
    public ProductService(IProductRepository repo) => _repo = repo;

    public Task<List<Product>> ListAsync(string? q = null, CancellationToken ct = default)
        => _repo.ListAsync(q, ct);

    public Task CreateAsync(string name, decimal unitPrice, CancellationToken ct = default)
        => _repo.AddAsync(new Product(name, unitPrice), ct);
}
