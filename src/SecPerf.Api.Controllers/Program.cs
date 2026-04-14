using SecPerf.Application.Extensions;
using SecPerf.ApiMvc.Extensions;
using SecPerf.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container (controllers-only API)
builder.Services.AddControllers();

// Application and infrastructure registrations
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// JWT and Swagger for API
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Configure the HTTP request pipeline.
app = app.UseSwaggerDocumentation();

if (app.Environment.IsDevelopment())
{
    // dev-specific configuration
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
