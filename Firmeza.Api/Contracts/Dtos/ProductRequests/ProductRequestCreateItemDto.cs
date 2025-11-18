namespace Firmeza.Api.Contracts.Dtos.ProductRequests;

public class ProductRequestCreateItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
