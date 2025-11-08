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
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _db;
        public ProductRepository(AppDbContext db) => _db = db;

        public Task<Product?> GetAsync(Guid id, string? ownerId = null) =>
            _db.Products.FirstOrDefaultAsync(p => p.Id == id && (ownerId == null || p.CreatedByUserId == ownerId));

        public async Task<List<Product>> ListAsync(string? ownerId = null)
        {
            var query = _db.Products.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(ownerId))
                query = query.Where(p => p.CreatedByUserId == ownerId);
            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        public Task<List<Product>> ListActiveAsync() =>
            _db.Products.AsNoTracking()
                .Where(p => p.IsActive && p.Stock > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task CreateAsync(Product p)
        {
            _db.Products.Add(p);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product p, string? ownerId = null)
        {
            var existing = await _db.Products.FirstOrDefaultAsync(x => x.Id == p.Id && (ownerId == null || x.CreatedByUserId == ownerId));
            if (existing == null) return;

            existing.Name = p.Name;
            existing.UnitPrice = p.UnitPrice;
            existing.Stock = p.Stock;
            existing.IsActive = p.IsActive;
            await _db.SaveChangesAsync();
        }

        public async Task<ProductDeleteResult> DeleteAsync(Guid id, bool force = false, string? ownerId = null)
        {
            var result = new ProductDeleteResult();
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && (ownerId == null || p.CreatedByUserId == ownerId));
            if (product == null) return result;

            var saleItems = await _db.SaleItems
                .Include(si => si.Sale)
                .Where(si => si.ProductId == id && (ownerId == null || si.Sale!.CreatedByUserId == ownerId))
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
                var saleIds = saleItems.Select(si => si.SaleId).Distinct().ToList();
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
}
