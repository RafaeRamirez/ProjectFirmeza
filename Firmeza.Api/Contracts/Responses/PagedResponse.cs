namespace Firmeza.Api.Contracts.Responses;

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalItems)
{
    public long TotalPages => (long)Math.Ceiling(TotalItems / (double)PageSize);
}
