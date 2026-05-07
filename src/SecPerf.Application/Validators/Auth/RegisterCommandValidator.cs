using FluentValidation;
using SecPerf.Application.Features.Auth.Commands.Register;

namespace SecPerf.Application.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(200);
    }
}
