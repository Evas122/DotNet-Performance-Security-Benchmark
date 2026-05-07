using FluentValidation;
using SecPerf.Application.Features.Auth.Commands.Login;

namespace SecPerf.Application.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(8);
    }
}
