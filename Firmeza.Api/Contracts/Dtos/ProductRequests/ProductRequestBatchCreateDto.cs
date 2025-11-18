namespace Firmeza.Api.Contracts.Dtos.ProductRequests;

public class ProductRequestBatchCreateDto
{
    public List<ProductRequestCreateItemDto> Items { get; set; } = new();
}
