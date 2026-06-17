namespace WebAPI.ExceptionHandlers;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

using Shared.Exceptions;

public class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext       httpContext,
        Exception         exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException e      => (StatusCodes.Status404NotFound, e.Message),
            BusinessRuleException e  => (StatusCodes.Status422UnprocessableEntity, e.Message),
            IllegalValuesException e => (StatusCodes.Status400BadRequest, e.Message),
            _                        => (0, null)
        };

        if (status == 0)
        {
            return false; // nicht behandelt → nächster Handler oder Default
        }

        httpContext.Response.StatusCode = status;
        await Results.Problem(statusCode: status, title: title)
            .ExecuteAsync(httpContext);
        return true;
    }
}