using MediatR;
using SecPerf.Application.Dtos.Product;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(System.Guid Id, UpdateProductRequest Request) : IRequest<Result<ProductDto>>;
