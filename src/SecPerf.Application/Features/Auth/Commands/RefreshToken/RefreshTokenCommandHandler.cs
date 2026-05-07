using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SecPerf.Application.Dtos.Auth;
using SecPerf.Application.Interfaces;
using SecPerf.Domain.Common;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwt;

    public RefreshTokenCommandHandler(IUnitOfWork uow, IJwtTokenService jwt)
    {
        _uow = uow;
        _jwt = jwt;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken);
        if (existing == null || !existing.IsActive) return Result.Fail<AuthResponse>("InvalidToken", "Refresh token invalid or expired");

        // revoke old token
        existing.Revoke();
        await _uow.RefreshTokens.UpdateAsync(existing);

        var user = await _uow.Users.GetByIdAsync(existing.UserId);
        if (user == null) return Result.Fail<AuthResponse>("InvalidToken", "Associated user not found");

        var accessToken = _jwt.GenerateAccessToken(user, out var accessExpiresAt);
        var refresh = _jwt.GenerateRefreshToken();

        var newRefresh = new SecPerf.Domain.Entities.RefreshToken
        {
            Id = System.Guid.NewGuid(),
            Token = refresh.Token,
            CreatedAt = System.DateTime.UtcNow,
            ExpiresAt = refresh.ExpiresAt,
            Revoked = false,
            UserId = user.Id
        };

        await _uow.RefreshTokens.AddAsync(newRefresh);
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
