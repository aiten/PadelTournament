namespace WebAPI.Filters;

using System.Linq;
using System.Threading.Tasks;

using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public class ValidationFilter<T>(IValidator<T>? validator = null) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate          next)
    {
        if (validator is null)
        {
            return await next(context);
        }

        var obj = context.Arguments.OfType<T>().FirstOrDefault();
        if (obj is null)
        {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(obj);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<ValidationFilter<T>>();
    }
}