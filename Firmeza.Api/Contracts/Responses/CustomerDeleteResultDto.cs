namespace Firmeza.Api.Contracts.Responses;

public class CustomerDeleteResultDto
{
    public bool Removed { get; set; }
    public bool NotFound { get; set; }
    public bool HasSales { get; set; }
}
