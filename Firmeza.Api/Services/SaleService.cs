using AutoMapper;
using Firmeza.Api.Contracts.Dtos.Sales;
using Firmeza.Api.Data;
using Firmeza.Api.Domain.Entities;
using Firmeza.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Services;

public class SaleService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SaleService> _logger;

    public SaleService(AppDbContext db, IMapper mapper, IEmailSender emailSender, ILogger<SaleService> logger)
    {
        _db = db;
        _mapper = mapper;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<List<SaleDto>> ListAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = BuildSaleQuery();

        if (from.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.CreatedAt <= to.Value);
        }

        var sales = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return _mapper.Map<List<SaleDto>>(sales);
    }

    public async Task<SaleDto?> GetAsync(Guid id)
    {
        var sale = await BuildSaleQuery().FirstOrDefaultAsync(s => s.Id == id);
        return sale is null ? null : _mapper.Map<SaleDto>(sale);
    }

    public async Task<SaleDto> CreateAsync(SaleCreateDto dto, string userId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == dto.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new ArgumentException("El cliente indicado no existe", nameof(dto.CustomerId));
        }

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new ArgumentException("Uno o más productos no existen", nameof(dto.Items));
        }

        var sale = new Sale
        {
            CustomerId = dto.CustomerId,
            CreatedByUserId = userId
        };

        foreach (var item in dto.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            if (!product.IsActive || product.Stock < item.Quantity)
            {
                throw new InvalidOperationException($"El producto {product.Name} no tiene stock suficiente.");
            }

            product.Stock -= item.Quantity;
            var saleItem = new SaleItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.UnitPrice,
                Subtotal = product.UnitPrice * item.Quantity
            };
            sale.Items.Add(saleItem);
        }

        sale.Total = sale.Items.Sum(i => i.Subtotal);
        _db.Sales.Add(sale);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(sale).Reference(s => s.Customer).LoadAsync(cancellationToken);
        await _db.Entry(sale).Collection(s => s.Items).LoadAsync(cancellationToken);
        foreach (var item in sale.Items)
        {
            await _db.Entry(item).Reference(i => i.Product).LoadAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            await TrySendSaleConfirmationAsync(customer.Email!, sale, cancellationToken);
        }

        return _mapper.Map<SaleDto>(sale);
    }

    private IQueryable<Sale> BuildSaleQuery() => _db.Sales
        .AsNoTracking()
        .Include(s => s.Customer)
        .Include(s => s.Items)
            .ThenInclude(i => i.Product);

    private async Task TrySendSaleConfirmationAsync(string email, Sale sale, CancellationToken ct)
    {
        try
        {
            var subject = $"Confirmación de compra #{sale.Id.ToString()[..8]}";
            var lines = sale.Items
                .Select(i => $"<li>{i.Product?.Name} x{i.Quantity} - {i.Subtotal:C}</li>");
            var html = $"""
                <p>Gracias por tu compra.</p>
                <p>Total: {sale.Total:C}</p>
                <ul>{string.Join(string.Empty, lines)}</ul>
                """;
            await _emailSender.SendAsync(email, subject, html, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar el correo de confirmación.");
        }
    }
}
