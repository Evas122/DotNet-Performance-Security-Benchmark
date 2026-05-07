using MediatR;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Auth.Commands.RevokeToken;

public record RevokeTokenCommand(string RefreshToken) : IRequest<Result>;
