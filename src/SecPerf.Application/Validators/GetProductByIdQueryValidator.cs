using FluentValidation;
using SecPerf.Application.Features.Products.Queries.GetProductById;

namespace SecPerf.Application.Validators;

public class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
