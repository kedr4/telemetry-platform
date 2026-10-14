using Npgsql;

namespace Telemetry.Gateway.Infrastructure.Persistence;

/// <summary>A parameterized SQL statement.</summary>
/// <param name="Text">SQL text; contains only constant fragments.</param>
/// <param name="Parameters">Values referenced by the text.</param>
internal sealed record SqlStatement(string Text, IReadOnlyList<NpgsqlParameter> Parameters);
