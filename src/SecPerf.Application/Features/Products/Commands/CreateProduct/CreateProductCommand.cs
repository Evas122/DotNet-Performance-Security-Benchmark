using MediatR;
using SecPerf.Application.Dtos.Product;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(CreateProductRequest Request) : IRequest<Result<ProductDto>>;
