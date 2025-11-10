using AutoMapper;
using Firmeza.Api.Contracts.Dtos.Customers;
using Firmeza.Api.Contracts.Dtos.Products;
using Firmeza.Api.Contracts.Dtos.Sales;
using Firmeza.Api.Domain.Entities;

namespace Firmeza.Api.Mapping;

public class DomainProfile : Profile
{
    public DomainProfile()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<ProductCreateDto, Product>();
        CreateMap<ProductUpdateDto, Product>();

        CreateMap<Customer, CustomerDto>();
        CreateMap<CustomerCreateDto, Customer>();
        CreateMap<CustomerUpdateDto, Customer>();

        CreateMap<SaleItem, SaleItemDto>()
            .ForCtorParam(nameof(SaleItemDto.ProductName), opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty));

        CreateMap<Sale, SaleDto>();
    }
}
