using MediatR;
using SecPerf.Application.Dtos.Auth;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;
