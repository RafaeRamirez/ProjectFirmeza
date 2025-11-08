using System;
using System.Linq;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;
namespace Firmeza.Web.Services
{
    public class SaleService
    {
        private readonly ISaleRepository _repo;
        public SaleService(ISaleRepository repo) => _repo = repo;

        public Task<List<Sale>> ListAsync(
            DateTime? from = null,
            DateTime? to = null,
            Guid? customerId = null,
            decimal? minTotal = null,
            decimal? maxTotal = null,
            string? ownerId = null) =>
            _repo.ListAsync(from, to, customerId, minTotal, maxTotal, ownerId);

        public Task<Sale?> GetAsync(Guid id, string? ownerId = null) => _repo.GetAsync(id, ownerId);

        public async Task CreateAsync(Sale sale)
        {
            sale.CreatedAt = DateTime.UtcNow;
            sale.Total = sale.Items.Sum(i => i.Subtotal);
            await _repo.CreateAsync(sale);
        }

        public async Task UpdateAsync(Sale sale, string? ownerId = null)
        {
            sale.Total = sale.Items.Sum(i => i.Subtotal);
            await _repo.UpdateAsync(sale, ownerId);
        }

        public Task DeleteAsync(Guid id, string? ownerId = null) => _repo.DeleteAsync(id, ownerId);
    }
}
