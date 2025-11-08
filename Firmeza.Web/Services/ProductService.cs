using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;

namespace Firmeza.Web.Services
{
    public class ProductService
    {
        private readonly IProductRepository _repo;
        private readonly IStringSanitizer _san;
        public ProductService(IProductRepository repo, IStringSanitizer san){ _repo=repo; _san=san; }

        public Task<List<Product>> ListAsync(string? ownerId=null)=>_repo.ListAsync(ownerId);
        public Task<Product?> GetAsync(Guid id, string? ownerId=null)=>_repo.GetAsync(id, ownerId);
        public async Task CreateAsync(Product p, string ownerId){ p.Name=_san.Clean(p.Name); p.CreatedByUserId=ownerId; await _repo.CreateAsync(p);}
        public async Task UpdateAsync(Product p, string? ownerId=null){ p.Name=_san.Clean(p.Name); await _repo.UpdateAsync(p, ownerId);}
        public Task<ProductDeleteResult> DeleteAsync(Guid id, bool force=false, string? ownerId=null)=>_repo.DeleteAsync(id, force, ownerId);
    }
}
