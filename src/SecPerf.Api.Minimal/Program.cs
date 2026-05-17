using AspNetCoreRateLimit;
using SecPerf.Application.Extensions;
using SecPerf.ApiMinimal.Extensions;
using SecPerf.ApiMinimal.Endpoints;
using SecPerf.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Application and infrastructure registrations (centralized)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Bearer authentication + authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// Swagger / OpenAPI
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Global exception handler returning ProblemDetails
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        context.Response.ContentType = "application/problem+json";

        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var pd = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "An unexpected error occurred",
            Detail = ex?.Message,
            Status = StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = pd.Status.Value;
        await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, pd, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
    });
});

// Security headers (NWebsec)
app.UseXContentTypeOptions();
app.UseReferrerPolicy(opts => opts.NoReferrer());
app.UseXXssProtection(options => options.EnabledWithBlockMode());
app.UseXfo(options => options.Deny());
app.UseCsp(csp => csp.DefaultSources(d => d.Self()).ScriptSources(s => s.Self()));

// Rate limiting
app.UseMiddleware<AspNetCoreRateLimit.IpRateLimitMiddleware>();

// Swagger
app = app.UseSwaggerDocumentation();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map minimal API endpoints
app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapHealthEndpoints();

app.Run();
