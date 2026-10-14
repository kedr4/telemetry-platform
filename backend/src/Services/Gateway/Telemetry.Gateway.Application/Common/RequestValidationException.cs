namespace Telemetry.Gateway.Application.Common;

/// <summary>A request was rejected by validation. Mapped to HTTP 400 / a GraphQL validation error.</summary>
public sealed class RequestValidationException : Exception
{
    public RequestValidationException()
        : this(new Dictionary<string, string[]>())
    {
    }

    public RequestValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public RequestValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    /// <summary>Gets the error messages per invalid property.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
