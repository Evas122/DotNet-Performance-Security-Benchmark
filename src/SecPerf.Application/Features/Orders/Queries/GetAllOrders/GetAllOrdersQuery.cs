using System.Collections.Generic;
using MediatR;
using SecPerf.Application.Dtos.Order;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Orders.Queries.GetAllOrders;

public record GetAllOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<Result<IEnumerable<OrderDto>>>;
