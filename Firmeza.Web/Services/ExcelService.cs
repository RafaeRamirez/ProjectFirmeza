using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;

namespace Firmeza.Web.Services
{
    public class ExcelService : IExcelService
    {
        public Task<byte[]> ExportProductsAsync(IEnumerable<Product> items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Productos");
            ws.Cells[1, 1].Value = "Nombre";
            ws.Cells[1, 2].Value = "Precio";
            ws.Cells[1, 3].Value = "Stock";
            ws.Cells[1, 4].Value = "Activo";
            int r = 2;
            foreach (var p in items)
            {
                ws.Cells[r, 1].Value = p.Name;
                ws.Cells[r, 2].Value = p.UnitPrice;
                ws.Cells[r, 3].Value = p.Stock;
                ws.Cells[r, 4].Value = p.IsActive ? "Sí" : "No";
                r++;
            }
            return pkg.GetAsByteArrayAsync();
        }

        public Task<(List<Product> ok, List<string> errors)> ImportProductsAsync(System.IO.Stream stream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var ok = new List<Product>();
            var errors = new List<string>();
            using var pkg = new ExcelPackage(stream);
            var ws = pkg.Workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                errors.Add("No worksheet found.");
                return Task.FromResult((ok, errors));
            }

            int row = 2;
            while (true)
            {
                var name = ws.Cells[row, 1].GetValue<string>();
                if (string.IsNullOrWhiteSpace(name))
                    break;

                var price = ws.Cells[row, 2].GetValue<decimal>();
                var stock = ws.Cells[row, 3].GetValue<int>();
                var activeText = (ws.Cells[row, 4].GetValue<string>() ?? "Sí").Trim().ToLower();
                var active = activeText.StartsWith("s");
                ok.Add(new Product
                {
                    Name = name.Trim(),
                    UnitPrice = price,
                    Stock = stock,
                    IsActive = active
                });
                row++;
        }

            return Task.FromResult((ok, errors));
        }

        public Task<byte[]> ExportSalesAsync(IEnumerable<Sale> sales)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Ventas");
            ws.Cells[1, 1].Value = "Fecha";
            ws.Cells[1, 2].Value = "Cliente";
            ws.Cells[1, 3].Value = "Total";
            ws.Cells[1, 4].Value = "Items";
            int r = 2;
            foreach (var sale in sales)
            {
                ws.Cells[r, 1].Value = sale.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                ws.Cells[r, 2].Value = sale.Customer?.FullName ?? "-";
                ws.Cells[r, 3].Value = sale.Total;
                ws.Cells[r, 4].Value = sale.Items.Count;
                r++;
            }
            return pkg.GetAsByteArrayAsync();
        }

        public Task<byte[]> ExportCustomersAsync(IEnumerable<Customer> customers)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Clientes");
            ws.Cells[1, 1].Value = "Nombre";
            ws.Cells[1, 2].Value = "Correo";
            ws.Cells[1, 3].Value = "Teléfono";
            int row = 2;
            foreach (var customer in customers)
            {
                ws.Cells[row, 1].Value = customer.FullName;
                ws.Cells[row, 2].Value = customer.Email;
                ws.Cells[row, 3].Value = customer.Phone;
                row++;
            }
            return pkg.GetAsByteArrayAsync();
        }

        public Task<(List<Customer> ok, List<string> errors)> ImportCustomersAsync(System.IO.Stream stream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var ok = new List<Customer>();
            var errors = new List<string>();
            using var pkg = new ExcelPackage(stream);
            var ws = pkg.Workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                errors.Add("El archivo no contiene hojas.");
                return Task.FromResult((ok, errors));
            }

            var totalRows = ws.Dimension?.Rows ?? 0;
            for (int row = 2; row <= totalRows; row++)
            {
                var name = ws.Cells[row, 1].GetValue<string>()?.Trim();
                var email = ws.Cells[row, 2].GetValue<string>()?.Trim();
                var phone = ws.Cells[row, 3].GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"Fila {row}: el nombre es obligatorio.");
                    continue;
                }

                ok.Add(new Customer
                {
                    FullName = name!,
                    Email = email,
                    Phone = phone
                });
            }

            return Task.FromResult((ok, errors));
        }
    }
}
