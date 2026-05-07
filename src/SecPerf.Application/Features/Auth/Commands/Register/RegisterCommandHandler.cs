using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SecPerf.Application.Dtos.Auth;
using SecPerf.Application.Interfaces;
using SecPerf.Domain.Common;
using SecPerf.Domain.Entities;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwt;
    private readonly SecPerf.Application.Interfaces.IPasswordHasher _hasher;

    public RegisterCommandHandler(IUnitOfWork uow, IJwtTokenService jwt, SecPerf.Application.Interfaces.IPasswordHasher hasher)
    {
        _uow = uow;
        _jwt = jwt;
        _hasher = hasher;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var existing = await _uow.Users.GetByEmailAsync(r.Email);
        if (existing != null) return Result.Fail<AuthResponse>("Conflict", "Email already in use");

        var user = new User
        {
            Id = System.Guid.NewGuid(),
            Email = r.Email,
            FirstName = r.FirstName,
            LastName = r.LastName,
            PasswordHash = _hasher.Hash(r.Password),
            Role = UserRole.Customer,
            CreatedAt = System.DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user);
        await _uow.CommitAsync();

        // generate tokens
        var accessToken = _jwt.GenerateAccessToken(user, out var accessExpiresAt);
        var refresh = _jwt.GenerateRefreshToken();

        var refreshToken = new SecPerf.Domain.Entities.RefreshToken
        {
            Id = System.Guid.NewGuid(),
            Token = refresh.Token,
            CreatedAt = System.DateTime.UtcNow,
            ExpiresAt = refresh.ExpiresAt,
            Revoked = false,
            UserId = user.Id
        };

        await _uow.RefreshTokens.AddAsync(refreshToken);
        await _uow.CommitAsync();

        var resp = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refresh.Token,
            ExpiresIn = (int)(accessExpiresAt - System.DateTime.UtcNow).TotalSeconds
        };

        return Result.Ok(resp);
    }
}
