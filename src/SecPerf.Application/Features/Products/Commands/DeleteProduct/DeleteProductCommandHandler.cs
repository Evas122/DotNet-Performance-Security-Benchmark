using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SecPerf.Domain.Common;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _uow;

    public DeleteProductCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var existing = await _uow.Products.GetByIdAsync(request.Id);
        if (existing == null)
        {
            return Result.Fail("NotFound", "Product not found");
        }

        existing.SoftDelete();
        await _uow.Products.UpdateAsync(existing);
        await _uow.CommitAsync();

        return Result.Ok();
    }
}
