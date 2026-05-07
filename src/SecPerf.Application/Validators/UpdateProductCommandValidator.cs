using FluentValidation;
using SecPerf.Application.Features.Products.Commands.UpdateProduct;

namespace SecPerf.Application.Validators;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Description).MaximumLength(2000).When(x => x.Request.Description != null);
        RuleFor(x => x.Request.Price).GreaterThan(0);
        RuleFor(x => x.Request.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.CategoryId).NotEmpty();
    }
}
