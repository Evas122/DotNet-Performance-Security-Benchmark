using FluentValidation;
using SecPerf.Application.Features.Products.Commands.CreateProduct;

namespace SecPerf.Application.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Description).MaximumLength(2000).When(x => x.Request.Description != null);
        RuleFor(x => x.Request.Price).GreaterThan(0);
        RuleFor(x => x.Request.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.CategoryId).NotEmpty();
    }
}
