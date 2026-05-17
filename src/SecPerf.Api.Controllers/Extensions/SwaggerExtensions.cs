using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SecPerf.ApiMvc.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.ParameterLocation.Header,
                Description = "Enter your JWT access token."
            });
            c.AddSecurityRequirement(doc =>
            {
                var requirement = new Microsoft.OpenApi.OpenApiSecurityRequirement();
                requirement[new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>();
                return requirement;
            });
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        // Enable swagger middleware and UI so API documentation is available at /swagger
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecPerf API V1");
            // keep default RoutePrefix ("swagger") so UI is available at /swagger
        });

        return app;
    }
}
