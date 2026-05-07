using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SecPerf.Application.Dtos.Product;
using SecPerf.Domain.Common;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateProductCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var existing = await _uow.Products.GetByIdAsync(request.Id);
        if (existing == null)
        {
            return Result.Fail<ProductDto>("NotFound", "Product not found");
        }

        var dto = request.Request;
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Price = dto.Price;
        existing.Stock = dto.Stock;
        existing.CategoryId = dto.CategoryId;

        await _uow.Products.UpdateAsync(existing);
        await _uow.CommitAsync();

        var result = new ProductDto
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            Price = existing.Price,
            Stock = existing.Stock,
            CategoryId = existing.CategoryId
        };

        return Result.Ok(result);
    }
}
