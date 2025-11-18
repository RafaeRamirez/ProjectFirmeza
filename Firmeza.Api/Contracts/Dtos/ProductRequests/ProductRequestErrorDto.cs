namespace Firmeza.Api.Contracts.Dtos.ProductRequests;

public class ProductRequestErrorDto
{
    public Guid ProductId { get; set; }
    public string Message { get; set; } = string.Empty;
}
