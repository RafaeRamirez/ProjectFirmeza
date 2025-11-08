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
        private readonly ILogger<ProductRequestService> _logger;
        public ProductRequestService(AppDbContext db, IEmailSender email, ILogger<ProductRequestService> logger)
        {
            _db = db;
            _email = email;
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

        public Task<List<ProductRequest>> ListAsync() =>
            _db.ProductRequests.Include(r => r.Product)
                .AsNoTracking()
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

        public Task<List<ProductRequest>> ListByUserAsync(string userId) =>
            _db.ProductRequests.Include(r => r.Product)
                .AsNoTracking()
                .Where(r => r.RequestedByUserId == userId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

        public async Task<bool> UpdateStatusAsync(Guid id, string status, string? message, string processedByUserId)
        {
            var request = await _db.ProductRequests.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return false;

            if (status == "Approved")
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
                if (product == null || product.Stock < request.Quantity)
                {
                    status = "Rejected";
                    message = "Stock insuficiente.";
                }
                else
                {
                    product.Stock -= request.Quantity;
                    if (product.Stock <= 0)
                        product.IsActive = false;
                }
            }

            request.Status = status;
            request.ResponseMessage = message;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = processedByUserId;
            await _db.SaveChangesAsync();

            await NotifyAsync(request);
            return true;
        }

        private async Task NotifyAsync(ProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestedByEmail))
                return;

            var subject = $"Solicitud de {request.Product?.Name ?? "producto"} - {request.Status}";
            var body = $"Hola,<br/>Tu solicitud del producto <strong>{request.Product?.Name}</strong> fue marcada como <strong>{request.Status}</strong>.";
            if (!string.IsNullOrWhiteSpace(request.ResponseMessage))
                body += $"<br/><em>Nota:</em> {request.ResponseMessage}";
            body += "<br/><br/>Equipo Firmeza";

            try
            {
                await _email.SendAsync(request.RequestedByEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo notificar al cliente sobre la solicitud {RequestId}", request.Id);
            }
        }
    }
}
