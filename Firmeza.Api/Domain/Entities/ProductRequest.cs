namespace Firmeza.Api.Domain.Entities;

public class ProductRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public string RequestedByEmail { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public string? ResponseMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedByUserId { get; set; }
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
}
