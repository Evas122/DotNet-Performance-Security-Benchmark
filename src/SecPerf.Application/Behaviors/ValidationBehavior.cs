using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using SecPerf.Domain.Common;

namespace SecPerf.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators == null || !_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);
            var failures = _validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                var message = string.Join("; ", failures.Select(f => f.ErrorMessage));

                // If response type is Result (non-generic)
                if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Fail("Validation", message);
                }

                // If response type is Result<T>
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var valueType = typeof(TResponse).GetGenericArguments()[0];
                    var failMethod = typeof(Result).GetMethod("Fail", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null);
                    if (failMethod != null)
                    {
                        var generic = failMethod.MakeGenericMethod(valueType);
                        var obj = generic.Invoke(null, new object[] { "Validation", message });
                        return (TResponse)obj!;
                    }
                }

                // Fallback: throw validation exception
                throw new ValidationException(failures);
            }

            return await next();
        }
    }
}
