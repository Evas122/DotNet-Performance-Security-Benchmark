using MediatR;
using SecPerf.Application.Dtos.Product;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Products.Queries.GetProductById;

public record GetProductByIdQuery(System.Guid Id) : IRequest<Result<ProductDto>>;
