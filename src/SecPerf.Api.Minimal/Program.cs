using AspNetCoreRateLimit;
using SecPerf.Application.Extensions;
using SecPerf.ApiMinimal.Extensions;
using SecPerf.ApiMinimal.Endpoints;
using SecPerf.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Application and infrastructure registrations (centralized)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Health checks
builder.Services.AddHealthChecks();

// JWT Bearer authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Swagger / OpenAPI
builder.Services.AddSwaggerDocumentation();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

// Map auth endpoints (minimal API)
app.MapAuthEndpoints();

// Map health/info endpoints
app.MapHealthEndpoints();

app.MapControllers();

app.Run();
