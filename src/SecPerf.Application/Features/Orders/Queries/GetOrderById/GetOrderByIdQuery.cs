using System;
using MediatR;
using SecPerf.Application.Dtos.Order;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<Result<OrderDto>>;
