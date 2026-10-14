using System.Data.Common;
using Npgsql;
using Telemetry.Gateway.Application.Abstractions;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Meters;
using Telemetry.Gateway.Domain.Metrics;

namespace Telemetry.Gateway.Infrastructure.Persistence;

/// <summary>
/// Builds the PostgreSQL statements that LINQ cannot express well (DISTINCT ON, date_trunc buckets).
/// Only constant, whitelisted fragments are concatenated; every user-supplied value is a parameter.
/// </summary>
internal static class ReadingSql
{
    public const int MaxAggregateGroups = 10_000;

    private const string ReadingColumns =
        "r.id, r.meter_id, m.location, m.type, r.recorded_at, r.energy, r.co2, r.pm25, r.humidity, r.motion_detected";

    public static SqlStatement LatestReadings(ReadingFilter filter)
    {
        var where = Where(filter, metricType: null);
        return new SqlStatement(
            $"""
            SELECT DISTINCT ON (r.meter_id) {ReadingColumns}
            FROM readings r
            JOIN meters m ON m.id = r.meter_id
            {where.Clause}
            ORDER BY r.meter_id, r.recorded_at DESC, r.id DESC
            """,
            where.Parameters);
    }

    public static SqlStatement Aggregates(AggregationSpec spec)
    {
        var where = Where(spec.Filter, spec.Metric?.MeasuredBy());
        var parameters = new List<NpgsqlParameter>(where.Parameters);

        var byLocation = spec.GroupBy.Contains(AggregationDimension.Location);
        var byType = spec.GroupBy.Contains(AggregationDimension.Type);
        var byTime = spec.GroupBy.Contains(AggregationDimension.Time);

        if (byTime)
        {
            parameters.Add(new NpgsqlParameter("bucket", BucketName(spec.Bucket!.Value)));
        }

        // Ordinal positions of the grouping columns in the select list.
        var groupColumns = string.Join(", ", new[] { (byLocation, "1"), (byType, "2"), (byTime, "3") }
            .Where(column => column.Item1)
            .Select(column => column.Item2));

        return new SqlStatement(
            $"""
            SELECT {(byLocation ? "x.location" : "NULL::text")},
                   {(byType ? "x.type" : "NULL::text")},
                   {(byTime ? "date_trunc(@bucket, x.recorded_at, 'UTC')" : "NULL::timestamptz")},
                   COUNT(*), AVG(x.value), MIN(x.value), MAX(x.value), SUM(x.value)
            FROM (
                SELECT m.location, m.type, r.recorded_at, {ValueExpression(spec.Metric)} AS value
                FROM readings r
                JOIN meters m ON m.id = r.meter_id
                {where.Clause}
            ) AS x
            GROUP BY {groupColumns}
            ORDER BY {groupColumns}
            LIMIT {MaxAggregateGroups}
            """,
            parameters);
    }

    public static ReadingRecord ReadReading(DbDataReader reader) => new(
        reader.GetInt64(0),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetFieldValue<DateTimeOffset>(4),
        reader.IsDBNull(5) ? null : reader.GetDouble(5),
        reader.IsDBNull(6) ? null : reader.GetInt32(6),
        reader.IsDBNull(7) ? null : reader.GetInt32(7),
        reader.IsDBNull(8) ? null : reader.GetInt32(8),
        reader.IsDBNull(9) ? null : reader.GetBoolean(9));

    public static AggregateResult ReadAggregate(DbDataReader reader) => new(
        reader.IsDBNull(0) ? null : reader.GetString(0),
        reader.IsDBNull(1) ? null : MeterTypeColumn.FromColumn(reader.GetString(1)),
        reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2),
        reader.GetInt64(3),
        reader.IsDBNull(4) ? null : reader.GetDouble(4),
        reader.IsDBNull(5) ? null : reader.GetDouble(5),
        reader.IsDBNull(6) ? null : reader.GetDouble(6),
        reader.IsDBNull(7) ? null : reader.GetDouble(7));

    private static (string Clause, IReadOnlyList<NpgsqlParameter> Parameters) Where(ReadingFilter filter, MeterType? metricType)
    {
        var conditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (filter.Locations.Count > 0)
        {
            conditions.Add("m.location = ANY(@locations)");
            parameters.Add(new NpgsqlParameter<string[]>("locations", filter.Locations.ToArray()));
        }

        if (filter.Types.Count > 0)
        {
            conditions.Add("m.type = ANY(@types)");
            parameters.Add(new NpgsqlParameter<string[]>("types", filter.Types.Select(MeterTypeColumn.ToColumn).ToArray()));
        }

        if (metricType is { } type)
        {
            conditions.Add("m.type = @metric_type");
            parameters.Add(new NpgsqlParameter("metric_type", MeterTypeColumn.ToColumn(type)));
        }

        if (filter.From is { } from)
        {
            conditions.Add("r.recorded_at >= @from");
            parameters.Add(new NpgsqlParameter("from", from.ToUniversalTime()));
        }

        if (filter.To is { } to)
        {
            conditions.Add("r.recorded_at < @to");
            parameters.Add(new NpgsqlParameter("to", to.ToUniversalTime()));
        }

        var clause = conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", conditions);
        return (clause, parameters);
    }

    private static string ValueExpression(Metric? metric) => metric switch
    {
        null => "NULL::double precision",
        Metric.Energy => "r.energy",
        Metric.Co2 => "r.co2::double precision",
        Metric.Pm25 => "r.pm25::double precision",
        Metric.Humidity => "r.humidity::double precision",
        Metric.Motion => "CASE WHEN r.motion_detected THEN 1.0 ELSE 0.0 END",
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unknown metric."),
    };

    private static string BucketName(TimeBucket bucket) => bucket switch
    {
        TimeBucket.Minute => "minute",
        TimeBucket.Hour => "hour",
        TimeBucket.Day => "day",
        _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, "Unknown time bucket."),
    };
}
