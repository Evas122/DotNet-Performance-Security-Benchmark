using FluentValidation;
using SecPerf.Application.Features.Auth.Commands.RevokeToken;

namespace SecPerf.Application.Validators;

public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
