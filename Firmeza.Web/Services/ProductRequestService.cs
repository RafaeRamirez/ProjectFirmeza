using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Firmeza.Web.Interfaces;

namespace Firmeza.Web.Services
{
    public class ProductRequestService
    {
        private readonly AppDbContext _db;
        private readonly IEmailSender _email;
        private readonly IPdfService _pdf;
        private readonly ILogger<ProductRequestService> _logger;
        public ProductRequestService(AppDbContext db, IEmailSender email, IPdfService pdf, ILogger<ProductRequestService> logger)
        {
            _db = db;
            _email = email;
            _pdf = pdf;
            _logger = logger;
        }

        public async Task<ProductRequest?> CreateAsync(Guid productId, int quantity, string? note, string userId, string? email)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && p.Stock > 0);
            if (product == null) return null;

            var request = new ProductRequest
            {
                ProductId = productId,
                Quantity = quantity,
                Note = note,
                RequestedByUserId = userId,
                RequestedByEmail = email ?? string.Empty
            };

            _db.ProductRequests.Add(request);
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task<List<ProductRequest>> ListAsync()
        {
            var requests = await _db.ProductRequests
                .Include(r => r.Product)
                .Include(r => r.Sale).ThenInclude(s => s.Customer)
                .AsNoTracking()
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            await PopulateRequesterNamesAsync(requests);
            return requests;
        }

        public async Task<List<ProductRequest>> ListByUserAsync(string userId)
        {
            var requests = await _db.ProductRequests
                .Include(r => r.Product)
                .Include(r => r.Sale).ThenInclude(s => s.Customer)
                .AsNoTracking()
                .Where(r => r.RequestedByUserId == userId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            await PopulateRequesterNamesAsync(requests);
            return requests;
        }

        public async Task<bool> UpdateStatusAsync(Guid id, string status, string? message, string processedByUserId)
        {
            var request = await _db.ProductRequests.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return false;
            Sale? generatedSale = null;

            if (status == "Approved")
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
                if (product == null || product.Stock < request.Quantity)
                {
                    status = "Rejected";
                    message = "Stock insuficiente.";
                    await RemoveSaleAsync(request);
                }
                else
                {
                    product.Stock -= request.Quantity;
                    if (product.Stock <= 0)
                        product.IsActive = false;

                    generatedSale = await CreateSaleAsync(request, product, processedByUserId);
                    request.SaleId = generatedSale?.Id;
                }
            }
            else
            {
                await RemoveSaleAsync(request);
            }

            request.Status = status;
            request.ResponseMessage = message;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = processedByUserId;
            await _db.SaveChangesAsync();

            await NotifyAsync(request, generatedSale);
            return true;
        }

        private async Task<Sale?> CreateSaleAsync(ProductRequest request, Product product, string processedByUserId)
        {
            Customer? customer = null;

            if (!string.IsNullOrWhiteSpace(request.RequestedByUserId))
            {
                customer = await _db.Customers.FirstOrDefaultAsync(c => c.CreatedByUserId == request.RequestedByUserId);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(request.RequestedByEmail))
            {
                var normalizedEmail = request.RequestedByEmail.Trim().ToLowerInvariant();
                customer = await _db.Customers
                    .FirstOrDefaultAsync(c =>
                        c.Email != null &&
                        c.Email.ToLower() == normalizedEmail &&
                        (c.CreatedByUserId == request.RequestedByUserId || c.CreatedByUserId == processedByUserId));

                if (customer == null)
                {
                    customer = await _db.Customers.FirstOrDefaultAsync(c =>
                        c.Email != null &&
                        c.Email.ToLower() == normalizedEmail);
                }
            }

            if (customer == null)
            {
                customer = new Customer
                {
                    FullName = request.RequestedByEmail ?? "Cliente Firmeza",
                    Email = request.RequestedByEmail,
                    CreatedByUserId = processedByUserId
                };
                _db.Customers.Add(customer);
            }

            var sale = new Sale
            {
                CustomerId = customer.Id,
                Customer = customer,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = processedByUserId,
                Items = new List<SaleItem>
                {
                    new SaleItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        Quantity = request.Quantity,
                        UnitPrice = product.UnitPrice,
                        Subtotal = product.UnitPrice * request.Quantity
                    }
                },
                Total = product.UnitPrice * request.Quantity
            };

            _db.Sales.Add(sale);
            return sale;
        }

        private async Task RemoveSaleAsync(ProductRequest request)
        {
            if (!request.SaleId.HasValue)
                return;

            var sale = await _db.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == request.SaleId.Value);
            if (sale != null)
            {
                _db.SaleItems.RemoveRange(sale.Items);
                _db.Sales.Remove(sale);
            }

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
            if (product != null)
            {
                product.Stock += request.Quantity;
                product.IsActive = true;
            }

            request.SaleId = null;
        }

        private async Task PopulateRequesterNamesAsync(List<ProductRequest> requests)
        {
            if (requests == null || requests.Count == 0)
                return;

            var ownerIds = requests
                .Select(r => r.RequestedByUserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

            Dictionary<string, string>? byOwner = null;
            if (ownerIds.Count > 0)
            {
                var ownerCustomers = await _db.Customers
                    .AsNoTracking()
                    .Where(c => ownerIds.Contains(c.CreatedByUserId))
                    .Select(c => new { c.CreatedByUserId, c.FullName })
                    .ToListAsync();

                byOwner = ownerCustomers
                    .GroupBy(c => c.CreatedByUserId)
                    .ToDictionary(g => g.Key, g => g.First().FullName, StringComparer.OrdinalIgnoreCase);
            }

            var targets = requests
                .Where(r => !string.IsNullOrWhiteSpace(r.RequestedByEmail))
                .Select(r => new
                {
                    Email = r.RequestedByEmail!.Trim().ToLowerInvariant(),
                    Owner = r.RequestedByUserId ?? string.Empty
                })
                .Distinct()
                .ToList();

            var emailKeys = targets.Select(t => t.Email).Distinct().ToList();

            Dictionary<string, string>? byComposite = null;
            Dictionary<string, string>? byEmail = null;

            if (emailKeys.Count > 0)
            {
                var customers = await _db.Customers
                    .AsNoTracking()
                    .Where(c => c.Email != null && emailKeys.Contains(c.Email.ToLower()))
                    .Select(c => new
                    {
                        Email = c.Email!,
                        c.FullName,
                        Owner = c.CreatedByUserId ?? string.Empty
                    })
                    .ToListAsync();

                byComposite = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                byEmail = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var customer in customers)
                {
                    var normalizedEmail = customer.Email.Trim().ToLowerInvariant();
                    var compositeKey = $"{normalizedEmail}|{customer.Owner}";
                    if (!byComposite.ContainsKey(compositeKey))
                    {
                        byComposite[compositeKey] = customer.FullName;
                    }

                    if (!byEmail.ContainsKey(normalizedEmail))
                    {
                        byEmail[normalizedEmail] = customer.FullName;
                    }
                }
            }

            foreach (var request in requests)
            {
                if (byOwner != null && !string.IsNullOrWhiteSpace(request.RequestedByUserId))
                {
                    if (byOwner.TryGetValue(request.RequestedByUserId, out var ownerName))
                    {
                        request.RequestedByName = ownerName;
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.RequestedByEmail))
                {
                    var emailKey = request.RequestedByEmail.Trim().ToLowerInvariant();
                    var ownerKey = request.RequestedByUserId ?? string.Empty;
                    var compositeKey = $"{emailKey}|{ownerKey}";

                    if (byComposite != null && byComposite.TryGetValue(compositeKey, out var ownerMatch))
                    {
                        request.RequestedByName = ownerMatch;
                        continue;
                    }

                    if (byEmail != null && byEmail.TryGetValue(emailKey, out var emailMatch))
                    {
                        request.RequestedByName = emailMatch;
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(request.RequestedByName) &&
                    !string.IsNullOrWhiteSpace(request.Sale?.Customer?.FullName))
                {
                    request.RequestedByName = request.Sale.Customer.FullName;
                }
            }
        }

        private async Task NotifyAsync(ProductRequest request, Sale? sale)
        {
            if (string.IsNullOrWhiteSpace(request.RequestedByEmail))
                return;

            var subject = $"Solicitud de {request.Product?.Name ?? "producto"} - {request.Status}";
            var body = $"Hola,<br/>Tu solicitud del producto <strong>{request.Product?.Name}</strong> fue marcada como <strong>{request.Status}</strong>.";
            if (!string.IsNullOrWhiteSpace(request.ResponseMessage))
                body += $"<br/><em>Nota:</em> {request.ResponseMessage}";
            body += "<br/><br/>Equipo Firmeza";

            IEnumerable<EmailAttachment>? attachments = null;
            if (request.Status == "Approved" && sale != null && sale.Customer != null)
            {
                try
                {
                    var pdfBytes = await _pdf.BuildReceiptAsync(sale, sale.Customer);
                    attachments = new[]
                    {
                        new EmailAttachment
                        {
                            FileName = $"recibo_{sale.Id}.pdf",
                            Content = pdfBytes,
                            ContentType = "application/pdf"
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo generar el recibo PDF para la solicitud {RequestId}", request.Id);
                }
            }

            try
            {
                await _email.SendAsync(request.RequestedByEmail, subject, body, attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo notificar al cliente sobre la solicitud {RequestId}", request.Id);
            }
        }
    }
}
