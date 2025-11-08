using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firmeza.Web.Models;
namespace Firmeza.Web.Interfaces{
 public interface IProductRepository{
 Task<List<Product>> ListAsync(string? ownerId = null);
 Task<List<Product>> ListActiveAsync();
  Task<Product?> GetAsync(Guid id, string? ownerId = null);
  Task CreateAsync(Product p);
  Task UpdateAsync(Product p, string? ownerId = null);
  Task<ProductDeleteResult> DeleteAsync(Guid id, bool force=false, string? ownerId = null);
 } }
