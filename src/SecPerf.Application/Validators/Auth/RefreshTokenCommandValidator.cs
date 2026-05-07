using FluentValidation;
using SecPerf.Application.Features.Auth.Commands.RefreshToken;

namespace SecPerf.Application.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
