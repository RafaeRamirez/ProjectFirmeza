namespace Firmeza.Api.Contracts.Dtos.Products;

public record ProductDto(Guid Id, string Name, decimal UnitPrice, int Stock, bool IsActive);
