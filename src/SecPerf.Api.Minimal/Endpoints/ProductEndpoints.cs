using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecPerf.Application.Dtos.Product;
using SecPerf.Application.Features.Products.Commands.CreateProduct;
using SecPerf.Application.Features.Products.Commands.DeleteProduct;
using SecPerf.Application.Features.Products.Commands.UpdateProduct;
using SecPerf.Application.Features.Products.Queries.GetAllProducts;
using SecPerf.Application.Features.Products.Queries.GetProductById;
using SecPerf.Domain.Common;

namespace SecPerf.ApiMinimal.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products");

        group.MapGet("/", async (ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new GetAllProductsQuery(page, pageSize));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error?.Message });
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetProductByIdQuery(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error?.Message });
        });

        group.MapPost("/", [Authorize] async (CreateProductRequest req, ISender sender) =>
        {
            var result = await sender.Send(new CreateProductCommand(req));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error?.Message });
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", [Authorize] async (Guid id, UpdateProductRequest req, ISender sender) =>
        {
            var result = await sender.Send(new UpdateProductCommand(id, req));
            if (result.IsSuccess) return Results.Ok(result.Value);
            return result.Error?.Code == "NotFound"
                ? Results.NotFound(new { error = result.Error.Message })
                : Results.BadRequest(new { error = result.Error?.Message });
        }).RequireAuthorization();

        group.MapDelete("/{id:guid}", [Authorize] async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id));
            return result.IsSuccess
                ? Results.Ok()
                : Results.NotFound(new { error = result.Error?.Message });
        }).RequireAuthorization();

        return app;
    }
}
