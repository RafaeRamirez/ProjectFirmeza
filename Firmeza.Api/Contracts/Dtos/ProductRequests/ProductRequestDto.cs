namespace Firmeza.Api.Contracts.Dtos.ProductRequests;

public class ProductRequestDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string? Note { get; init; }
    public string Status { get; init; } = "Pending";
    public string? ResponseMessage { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public Guid? SaleId { get; init; }
}
