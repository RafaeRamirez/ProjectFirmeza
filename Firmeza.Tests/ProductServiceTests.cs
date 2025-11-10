using AutoMapper;
using Firmeza.Api.Contracts.Dtos.Products;
using Firmeza.Api.Data;
using Firmeza.Api.Domain.Entities;
using Firmeza.Api.Mapping;
using Firmeza.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Firmeza.Tests;

public class ProductServiceTests
{
    private readonly IMapper _mapper;

    public ProductServiceTests()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<DomainProfile>());
        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public async Task OnlyAvailableFilter_ReturnsActiveProductsWithStock()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        context.Products.AddRange(
            new Product { Name = "Activo", UnitPrice = 10, Stock = 5, IsActive = true },
            new Product { Name = "Sin stock", UnitPrice = 5, Stock = 0, IsActive = true },
            new Product { Name = "Inactivo", UnitPrice = 15, Stock = 2, IsActive = false }
        );
        await context.SaveChangesAsync();

        var service = new ProductService(context, _mapper);
        var parameters = new ProductQueryParameters { OnlyAvailable = true, PageSize = 10 };
        var result = await service.SearchAsync(parameters);

        Assert.All(result.Items, p => Assert.True(p.IsActive && p.Stock > 0));
        Assert.Single(result.Items);
        Assert.Equal("Activo", result.Items[0].Name);
    }
}
