using Telemetry.Gateway.Application.Common;

namespace Telemetry.Gateway.Api.Rest;

/// <summary>
/// Parses enum query-string values case-insensitively (e.g. <c>?type=airQuality</c>), reporting invalid
/// values as validation errors so they surface as <c>400</c> like every other invalid input.
/// </summary>
internal static class QueryValues
{
    public static IReadOnlyList<TEnum> ParseEnums<TEnum>(string[]? values, string parameterName)
        where TEnum : struct, Enum =>
        values is null or { Length: 0 }
            ? []
            : values.Select(value => ParseEnum<TEnum>(value, parameterName)).ToList();

    public static TEnum? ParseOptionalEnum<TEnum>(string? value, string parameterName)
        where TEnum : struct, Enum =>
        string.IsNullOrEmpty(value) ? null : ParseEnum<TEnum>(value, parameterName);

    private static TEnum ParseEnum<TEnum>(string value, string parameterName)
        where TEnum : struct, Enum
    {
        // Numeric strings parse as enums too; only names are accepted.
        if (!int.TryParse(value, out _)
            && Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new RequestValidationException(new Dictionary<string, string[]>
        {
            [parameterName] = [$"'{value}' is not valid. Allowed values: {string.Join(", ", Enum.GetNames<TEnum>())}."],
        });
    }
}
