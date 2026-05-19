using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecPerf.Application.Features.Orders.Queries.GetAllOrders;
using SecPerf.Application.Features.Orders.Queries.GetOrderById;

namespace SecPerf.ApiMinimal.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").RequireAuthorization();

        group.MapGet("/", async (ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetAllOrdersQuery(page, pageSize));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error?.Message });
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error?.Message });
        });

        return app;
    }
}
