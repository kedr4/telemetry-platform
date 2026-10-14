using HotChocolate.Execution;
using Telemetry.Gateway.Application.Common;

namespace Telemetry.Gateway.Api.GraphQL;

/// <summary>
/// Exposes validation failures as GraphQL errors with code <c>VALIDATION_ERROR</c> and the messages per
/// field in <c>extensions.errors</c> (<c>[{ "field": "...", "messages": ["..."] }]</c>), instead of an
/// opaque "Unexpected Execution Error".
/// </summary>
internal sealed class ValidationErrorFilter : IErrorFilter
{
    public const string ValidationErrorCode = "VALIDATION_ERROR";

    public IError OnError(IError error)
    {
        if (error.Exception is not RequestValidationException validation)
        {
            return error;
        }

        return ErrorBuilder.FromError(error)
            .SetMessage(validation.Message)
            .SetCode(ValidationErrorCode)
            .SetExtension("errors", ToExtension(validation.Errors))
            .SetException(null)
            .Build();
    }

    // The GraphQL result serializer understands lists and string-keyed dictionaries of objects.
    private static List<Dictionary<string, object?>> ToExtension(IReadOnlyDictionary<string, string[]> errors) =>
        errors
            .Select(error => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["field"] = error.Key,
                ["messages"] = error.Value.ToList<object?>(),
            })
            .ToList();
}
