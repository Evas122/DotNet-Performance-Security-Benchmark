using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace SecPerf.ApiMinimal.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // simple warmup/health endpoint used by k6
        app.MapGet("/health", () => Results.Ok(new { timestamp = DateTime.UtcNow }));

        app.MapGet("/api/info", (IHostEnvironment env) =>
        {
            var entry = Assembly.GetEntryAssembly();
            var version = entry?.GetName()?.Version?.ToString() ?? "unknown";
            return Results.Ok(new
            {
                apiType = "minimal",
                version,
                environment = env.EnvironmentName
            });
        });

        return app;
    }
}
