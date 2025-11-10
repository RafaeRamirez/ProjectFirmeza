using Firmeza.Api.Contracts.Dtos.Customers;

namespace Firmeza.Api.Contracts.Dtos.Sales;

public record SaleDto(Guid Id, CustomerDto Customer, DateTime CreatedAt, decimal Total, IReadOnlyList<SaleItemDto> Items);
