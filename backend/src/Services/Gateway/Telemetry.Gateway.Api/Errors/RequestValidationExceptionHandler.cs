using Microsoft.AspNetCore.Diagnostics;
using Telemetry.Gateway.Application.Common;

namespace Telemetry.Gateway.Api.Errors;

/// <summary>Turns validation failures into <c>400 Bad Request</c> validation problem details.</summary>
internal sealed class RequestValidationExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not RequestValidationException validation)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new HttpValidationProblemDetails(validation.Errors.ToDictionary())
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request is invalid.",
            },
        });
    }
}
