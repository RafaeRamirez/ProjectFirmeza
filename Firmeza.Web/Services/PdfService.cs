using Firmeza.Web.Interfaces;
using Firmeza.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Linq;

namespace Firmeza.Web.Services
{
    public class PdfService : IPdfService
    {
        public Task<byte[]> BuildReceiptAsync(Sale sale, Customer customer)
        {
            var doc = Document.Create(c =>
            {
                c.Page(p =>
                {
                    p.Margin(40);
                    p.Header().Text("Recibo de Venta").SemiBold().FontSize(18).AlignCenter();
                    p.Content().Column(col =>
                    {
                        col.Item().Text($"Cliente: {customer.FullName}");
                        col.Item().Text($"Fecha: {sale.CreatedAt:yyyy-MM-dd HH:mm}");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(30);
                                cd.RelativeColumn();
                                cd.ConstantColumn(50);
                                cd.ConstantColumn(80);
                            });
                            t.Header(h =>
                            {
                                h.Cell().Text("#").SemiBold();
                                h.Cell().Text("Producto").SemiBold();
                                h.Cell().Text("Cant.").SemiBold();
                                h.Cell().Text("Subtotal").SemiBold();
                            });
                            for (int i = 0; i < sale.Items.Count; i++)
                            {
                                var it = sale.Items[i];
                                t.Cell().Text((i + 1).ToString());
                                t.Cell().Text(it.Product?.Name ?? "-");
                                t.Cell().Text(it.Quantity.ToString());
                                t.Cell().Text(it.Subtotal.ToString("0.00"));
                            }
                        });
                        col.Item().AlignRight().Text($"Total: {sale.Total:0.00}").SemiBold();
                    });
                });
            });
            return Task.FromResult(doc.GeneratePdf());
        }
    }
}
