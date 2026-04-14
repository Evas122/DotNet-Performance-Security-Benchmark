using SecPerf.Application.Extensions;
using SecPerf.ApiMinimal.Extensions;
using SecPerf.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Application and infrastructure registrations
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// JWT Bearer authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Swagger / OpenAPI
builder.Services.AddSwaggerDocumentation();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app = app.UseSwaggerDocumentation();

if (app.Environment.IsDevelopment())
{
    // additional dev-only configuration can go here
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
