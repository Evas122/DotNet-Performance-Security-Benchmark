using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecPerf.Application.Dtos.Auth;
using SecPerf.Application.Features.Auth.Commands.Login;
using SecPerf.Application.Features.Auth.Commands.Register;
using SecPerf.Application.Features.Auth.Commands.RefreshToken;
using SecPerf.Application.Features.Auth.Commands.RevokeToken;
using SecPerf.Domain.Common;

namespace SecPerf.ApiMinimal.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RegisterCommand(request));
            if (result == null) return Results.Problem("Null result from handler");
            if (result.IsFailure)
            {
                var code = result.Error?.Code;
                return code switch
                {
                    "Conflict" => Results.Conflict(new { error = result.Error?.Message }),
                    "Validation" => Results.BadRequest(new { error = result.Error?.Message }),
                    _ => Results.BadRequest(new { error = result.Error?.Message })
                };
            }

            return Results.Ok(result.Value);
        });

        group.MapPost("/login", async (LoginRequest request, ISender sender) =>
        {
            var result = await sender.Send(new LoginCommand(request));
            if (result == null) return Results.Problem("Null result from handler");
            if (result.IsFailure)
            {
                var code = result.Error?.Code;
                return code switch
                {
                    "InvalidCredentials" => Results.Unauthorized(),
                    "Validation" => Results.BadRequest(new { error = result.Error?.Message }),
                    _ => Results.BadRequest(new { error = result.Error?.Message })
                };
            }

            return Results.Ok(result.Value);
        });

        group.MapPost("/refresh", async (RefreshTokenRequestBody body, ISender sender) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(body.RefreshToken));
            if (result == null) return Results.Problem("Null result from handler");
            if (result.IsFailure)
            {
                var code = result.Error?.Code;
                return code switch
                {
                    "InvalidToken" => Results.BadRequest(new { error = result.Error?.Message }),
                    _ => Results.BadRequest(new { error = result.Error?.Message })
                };
            }

            return Results.Ok(result.Value);
        });

        group.MapPost("/revoke", [Authorize] async (RevokeTokenRequestBody body, ISender sender, HttpContext ctx) =>
        {
            var userIdStr = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? ctx.User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userIdStr, out var callerId)) return Results.Unauthorized();

            var result = await sender.Send(new RevokeTokenCommand(body.RefreshToken, callerId));
            if (result == null) return Results.Problem("Null result from handler");
            if (result.IsFailure)
            {
                return result.Error?.Code == "Forbidden"
                    ? Results.Json(new { error = result.Error.Message }, statusCode: 403)
                    : Results.BadRequest(new { error = result.Error?.Message });
            }

            return Results.Ok();
        }).RequireAuthorization();

        return app;
    }

    private record RefreshTokenRequestBody(string RefreshToken);
    private record RevokeTokenRequestBody(string RefreshToken);
}
