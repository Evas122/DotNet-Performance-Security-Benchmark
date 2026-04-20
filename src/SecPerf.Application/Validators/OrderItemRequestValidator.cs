using FluentValidation;
using SecPerf.Application.Dtos.Order;

namespace SecPerf.Application.Validators
{
    public class OrderItemRequestValidator : AbstractValidator<OrderItemRequestDto>
    {
        public OrderItemRequestValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }
}
