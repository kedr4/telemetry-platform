using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Telemetry.Gateway.Application.Abstractions;
using Telemetry.Gateway.Application.Common;
using Telemetry.Gateway.Application.Readings;
using Telemetry.Gateway.Domain.Aggregations;
using Telemetry.Gateway.Domain.Meters;
using Telemetry.Gateway.Domain.Readings;

namespace Telemetry.Gateway.Infrastructure.Persistence;

internal sealed class TelemetryReadStore(TelemetryReadDbContext db) : ITelemetryReadStore
{
    public async Task<IReadOnlyList<Meter>> GetMetersAsync(IReadOnlyCollection<MeterType> types, CancellationToken cancellationToken)
    {
        var query = db.Meters.AsQueryable();
        if (types.Count > 0)
        {
            var columns = types.Select(MeterTypeColumn.ToColumn).ToArray();
            query = query.Where(meter => columns.Contains(meter.Type));
        }

        var rows = await query
            .OrderBy(meter => meter.Location)
            .ThenBy(meter => meter.Type)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new Meter(row.Id, row.Location, MeterTypeColumn.FromColumn(row.Type)));
    }

    public async Task<IReadOnlyList<string>> GetLocationsAsync(CancellationToken cancellationToken) =>
        await db.Meters
            .Select(meter => meter.Location)
            .Distinct()
            .OrderBy(location => location)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reading>> GetLatestReadingsAsync(ReadingFilter filter, CancellationToken cancellationToken)
    {
        var records = await QueryAsync(ReadingSql.LatestReadings(filter), ReadingSql.ReadReading, cancellationToken);

        return records
            .OrderBy(record => record.Location, StringComparer.Ordinal)
            .ThenBy(record => record.Type, StringComparer.Ordinal)
            .Select(record => record.ToDomain())
            .ToList();
    }

    public async Task<ReadingSlice> GetReadingsAsync(
        ReadingFilter filter,
        ReadingCursor? after,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var filtered = Filter(db.Readings, filter);
        var totalCount = await filtered.LongCountAsync(cancellationToken);

        var page = filtered;
        if (after is { } cursor)
        {
            var (recordedAt, id) = (cursor.RecordedAt, cursor.Id);
            page = page.Where(reading => reading.RecordedAt < recordedAt
                || (reading.RecordedAt == recordedAt && reading.Id < id));
        }

        // One extra row tells whether another page follows.
        var records = await page
            .OrderByDescending(reading => reading.RecordedAt)
            .ThenByDescending(reading => reading.Id)
            .Take(pageSize + 1)
            .Select(reading => new ReadingRecord(
                reading.Id,
                reading.MeterId,
                reading.Meter.Location,
                reading.Meter.Type,
                reading.RecordedAt,
                reading.Energy,
                reading.Co2,
                reading.Pm25,
                reading.Humidity,
                reading.MotionDetected))
            .ToListAsync(cancellationToken);

        var items = records.Take(pageSize).Select(record => record.ToDomain()).ToList();
        return new ReadingSlice(items, totalCount, records.Count > pageSize);
    }

    public async Task<IReadOnlyList<AggregateResult>> GetAggregatesAsync(AggregationSpec spec, CancellationToken cancellationToken) =>
        await QueryAsync(ReadingSql.Aggregates(spec), ReadingSql.ReadAggregate, cancellationToken);

    private static IQueryable<ReadingRow> Filter(IQueryable<ReadingRow> readings, ReadingFilter filter)
    {
        if (filter.Locations.Count > 0)
        {
            var locations = filter.Locations.ToArray();
            readings = readings.Where(reading => locations.Contains(reading.Meter.Location));
        }

        if (filter.Types.Count > 0)
        {
            var types = filter.Types.Select(MeterTypeColumn.ToColumn).ToArray();
            readings = readings.Where(reading => types.Contains(reading.Meter.Type));
        }

        if (filter.From is { } from)
        {
            var fromUtc = from.ToUniversalTime();
            readings = readings.Where(reading => reading.RecordedAt >= fromUtc);
        }

        if (filter.To is { } to)
        {
            var toUtc = to.ToUniversalTime();
            readings = readings.Where(reading => reading.RecordedAt < toUtc);
        }

        return readings;
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "Statements are built by ReadingSql from constant fragments; all values are parameters.")]
    private async Task<List<T>> QueryAsync<T>(SqlStatement statement, Func<DbDataReader, T> read, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = statement.Text;
            command.Parameters.AddRange(statement.Parameters.ToArray());

            var results = new List<T>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(read(reader));
            }

            return results;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
