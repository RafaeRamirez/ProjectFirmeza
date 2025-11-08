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
    }
}
