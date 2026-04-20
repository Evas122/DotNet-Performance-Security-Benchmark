using AutoMapper;
using SecPerf.Application.Dtos.Category;
using SecPerf.Application.Dtos.Order;
using SecPerf.Application.Dtos.Product;
using SecPerf.Application.Dtos.User;
using SecPerf.Domain.Entities;

namespace SecPerf.Application.Mappings;

public class DtoMappingProfile : Profile
{
    public DtoMappingProfile()
    {
        // User
        CreateMap<User, UserResponseDto>();
        CreateMap<UserRequestDto, User>()
            // Password should be handled separately (hashing) so ignore direct mapping
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

        // Product
        CreateMap<Product, ProductResponseDto>().ReverseMap();
        CreateMap<ProductRequestDto, Product>();

        // Category
        CreateMap<Category, CategoryResponseDto>().ReverseMap();
        CreateMap<CategoryRequestDto, Category>();

        // Order and items
        CreateMap<OrderItem, OrderItemResponseDto>();
        CreateMap<OrderItemRequestDto, OrderItem>();

        CreateMap<Order, OrderResponseDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems));

        CreateMap<OrderRequestDto, Order>()
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.Items));
    }
}
