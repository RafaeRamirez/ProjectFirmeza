using System.Collections.Generic;
using System.IO;
using Firmeza.Web.Models;

namespace Firmeza.Web.Interfaces
{
    public interface IExcelService
    {
        Task<byte[]> ExportProductsAsync(IEnumerable<Product> items);
        Task<(List<Product> ok, List<string> errors)> ImportProductsAsync(Stream stream);
        Task<byte[]> ExportSalesAsync(IEnumerable<Sale> sales);
        Task<byte[]> ExportCustomersAsync(IEnumerable<Customer> customers);
        Task<(List<Customer> ok, List<string> errors)> ImportCustomersAsync(Stream stream);
    }
}
