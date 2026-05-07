using MediatR;
using SecPerf.Application.Dtos.Auth;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Features.Auth.Commands.Register;

public record RegisterCommand(RegisterRequest Request) : IRequest<Result<SecPerf.Application.Dtos.Auth.AuthResponse>>;
