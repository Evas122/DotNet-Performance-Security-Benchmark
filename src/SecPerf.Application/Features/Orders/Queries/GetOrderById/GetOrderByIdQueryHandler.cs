using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SecPerf.Application.Dtos.Order;
using SecPerf.Domain.Common;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IUnitOfWork _uow;

    public GetOrderByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = _uow.Orders
            .QueryWithItems()
            .Where(o => o.Id == request.Id)
            .Select(o => new OrderDto
            {
                Id         = o.Id,
                UserId     = o.UserId,
                CreatedAt  = o.CreatedAt,
                Status     = o.Status,
                TotalPrice = o.TotalPrice,
                Items      = o.OrderItems.Select(i => new OrderItemDto
                {
                    Id          = i.Id,
                    ProductId   = i.ProductId ?? default,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    Quantity    = i.Quantity,
                    UnitPrice   = i.UnitPrice,
                    LineTotal   = i.Quantity * i.UnitPrice,
                }).ToList()
            })
            .FirstOrDefault();

        if (order is null)
            return Task.FromResult(Result.Fail<OrderDto>("NotFound", "Order not found"));

        return Task.FromResult(Result.Ok(order));
    }
}
