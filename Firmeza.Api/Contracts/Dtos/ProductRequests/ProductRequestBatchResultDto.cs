namespace Firmeza.Api.Contracts.Dtos.ProductRequests;

public class ProductRequestBatchResultDto
{
    public List<ProductRequestDto> Requests { get; set; } = new();
    public List<ProductRequestErrorDto> Errors { get; set; } = new();
}
