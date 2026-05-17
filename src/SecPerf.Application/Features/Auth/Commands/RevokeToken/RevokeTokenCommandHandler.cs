using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SecPerf.Domain.Common;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Features.Auth.Commands.RevokeToken;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IUnitOfWork _uow;

    public RevokeTokenCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken);
        if (existing == null || !existing.IsActive) return Result.Fail("InvalidToken", "Refresh token invalid or already revoked");

        if (existing.UserId != request.CallerUserId)
            return Result.Fail("Forbidden", "You cannot revoke a refresh token that does not belong to you");

        existing.Revoke();
        await _uow.RefreshTokens.UpdateAsync(existing);
        await _uow.CommitAsync();

        return Result.Ok();
    }
}
