using FluentValidation;
using SecPerf.Application.Dtos.Category;

namespace SecPerf.Application.Validators
{
    public class CategoryRequestValidator : AbstractValidator<CategoryRequestDto>
    {
        public CategoryRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Description).MaximumLength(1000);
        }
    }
}
