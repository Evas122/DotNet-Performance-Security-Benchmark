using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SecPerf.Application.Dtos.Product;
using SecPerf.Domain.Common;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Features.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;

    public GetProductByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var p = await _uow.Products.GetByIdAsync(request.Id);
        if (p == null) return Result.Fail<ProductDto>("NotFound", "Product not found");

        var dto = new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            CategoryId = p.CategoryId
        };

        return Result.Ok(dto);
    }
}
