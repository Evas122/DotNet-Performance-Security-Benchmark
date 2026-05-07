using MediatR;
using SecPerf.Application.Dtos.Auth;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Auth.Commands.Login;

public record LoginCommand(LoginRequest Request) : IRequest<Result<AuthResponse>>;
