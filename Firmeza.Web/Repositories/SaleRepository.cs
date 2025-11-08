using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firmeza.Web.Data;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Web.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly AppDbContext _db;
        public SaleRepository(AppDbContext db) => _db = db;

        public async Task CreateAsync(Sale sale)
        {
            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();
        }

        public async Task<Sale?> GetAsync(Guid id, string? ownerId = null) =>
            await _db.Sales
                .Include(s => s.Customer)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id && (ownerId == null || s.CreatedByUserId == ownerId));

        public async Task<List<Sale>> ListAsync(
            DateTime? from = null,
            DateTime? to = null,
            Guid? customerId = null,
            decimal? minTotal = null,
            decimal? maxTotal = null,
            string? ownerId = null)
        {
            var query = _db.Sales
                .Include(s => s.Customer)
                .Include(s => s.Items)
                .AsQueryable();

            if (from.HasValue) query = query.Where(s => s.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(s => s.CreatedAt <= to.Value);
            if (customerId.HasValue) query = query.Where(s => s.CustomerId == customerId.Value);
            if (minTotal.HasValue) query = query.Where(s => s.Total >= minTotal.Value);
            if (maxTotal.HasValue) query = query.Where(s => s.Total <= maxTotal.Value);
            if (!string.IsNullOrWhiteSpace(ownerId)) query = query.Where(s => s.CreatedByUserId == ownerId);

            return await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        }

        public async Task UpdateAsync(Sale sale, string? ownerId = null)
        {
            var existing = await _db.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == sale.Id && (ownerId == null || s.CreatedByUserId == ownerId));

            if (existing == null) return;

            existing.CustomerId = sale.CustomerId;
            existing.Total = sale.Total;
            existing.CreatedAt = sale.CreatedAt;

            _db.SaleItems.RemoveRange(existing.Items);
            foreach (var item in sale.Items)
            {
                item.SaleId = existing.Id;
            }
            await _db.SaleItems.AddRangeAsync(sale.Items);

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, string? ownerId = null)
        {
            var sale = await _db.Sales.Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id && (ownerId == null || s.CreatedByUserId == ownerId));
            if (sale == null) return;

            _db.SaleItems.RemoveRange(sale.Items);
            _db.Sales.Remove(sale);
            await _db.SaveChangesAsync();
        }
    }
}
