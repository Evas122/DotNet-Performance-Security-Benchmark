using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SecPerf.Application.Dtos.Order;
using SecPerf.Domain.Common;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Features.Orders.Queries.GetAllOrders;

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, Result<IEnumerable<OrderDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAllOrdersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<Result<IEnumerable<OrderDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var page     = request.Page     < 1 ? 1  : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = _uow.Orders
            .QueryWithItems()
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            .ToList();

        return Task.FromResult(Result.Ok(items.AsEnumerable()));
    }
}
