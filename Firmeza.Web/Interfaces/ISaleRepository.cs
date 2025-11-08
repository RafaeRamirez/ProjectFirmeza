using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firmeza.Web.Models;

namespace Firmeza.Web.Interfaces
{
    public interface ISaleRepository
    {
        Task<List<Sale>> ListAsync(
            DateTime? from = null,
            DateTime? to = null,
            Guid? customerId = null,
            decimal? minTotal = null,
            decimal? maxTotal = null,
            string? ownerId = null);
        Task<Sale?> GetAsync(Guid id, string? ownerId = null);
        Task CreateAsync(Sale sale);
        Task UpdateAsync(Sale sale, string? ownerId = null);
        Task DeleteAsync(Guid id, string? ownerId = null);
    }
}
