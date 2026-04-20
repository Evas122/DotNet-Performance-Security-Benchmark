using FluentValidation;
using SecPerf.Application.Dtos.Order;

namespace SecPerf.Application.Validators
{
    public class OrderRequestValidator : AbstractValidator<OrderRequestDto>
    {
        public OrderRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());
        }
    }
}
