using MediatR;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Products.Commands.DeleteProduct;

public record DeleteProductCommand(System.Guid Id) : IRequest<Result>;
