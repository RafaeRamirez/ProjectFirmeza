namespace Firmeza.Api.Contracts.Dtos.Customers;

public record CustomerDto(Guid Id, string FullName, string? Email, string? Phone);
