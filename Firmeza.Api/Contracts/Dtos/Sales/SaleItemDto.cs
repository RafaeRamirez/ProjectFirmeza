namespace Firmeza.Api.Contracts.Dtos.Sales;

public record SaleItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);
