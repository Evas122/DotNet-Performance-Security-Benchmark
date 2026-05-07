using FluentValidation;
using SecPerf.Application.Features.Products.Commands.DeleteProduct;

namespace SecPerf.Application.Validators;

public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
