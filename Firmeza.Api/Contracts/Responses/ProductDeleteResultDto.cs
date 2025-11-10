namespace Firmeza.Api.Contracts.Responses;

public class ProductDeleteResultDto
{
    public bool Removed { get; set; }
    public bool SetInactive { get; set; }
    public bool HasSales { get; set; }
    public List<Guid> DeletedSales { get; } = new();
}
