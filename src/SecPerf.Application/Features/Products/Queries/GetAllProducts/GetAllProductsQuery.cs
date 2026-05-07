using System.Collections.Generic;
using MediatR;
using SecPerf.Application.Dtos.Product;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Products.Queries.GetAllProducts;

public record GetAllProductsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<IEnumerable<ProductDto>>>;
