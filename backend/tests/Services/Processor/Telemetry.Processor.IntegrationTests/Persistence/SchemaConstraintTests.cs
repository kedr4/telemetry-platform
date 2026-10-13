using Npgsql;
using Telemetry.Processor.IntegrationTests.Infrastructure;

namespace Telemetry.Processor.IntegrationTests.Persistence;

/// <summary>
/// The database itself rejects inconsistent rows, so data written by any client stays valid.
/// </summary>
public sealed class SchemaConstraintTests(MigratedDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("energy, motion_detected", "1, true", "ck_readings_single_measurement")]
    [InlineData("co2", "500", "ck_readings_single_measurement")]
    [InlineData("energy", "-1", "ck_readings_energy_non_negative")]
    [InlineData("co2, pm25, humidity", "500, 20, 120", "ck_readings_humidity_percentage")]
    public async Task InconsistentReading_IsRejected(string columns, string values, string constraint)
    {
        var (meterId, batchId) = await InsertMeterAndBatchAsync();

        var exception = await Should.ThrowAsync<PostgresException>(() => ExecuteAsync(
            $"INSERT INTO readings (meter_id, batch_id, recorded_at, {columns}) VALUES ({meterId}, '{batchId}', now(), {values})"));

        exception.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        exception.ConstraintName.ShouldBe(constraint);
    }

    [Fact]
    public async Task SameMeterTwiceInOneBatch_IsRejected()
    {
        var (meterId, batchId) = await InsertMeterAndBatchAsync();
        var insert = $"INSERT INTO readings (meter_id, batch_id, recorded_at, energy) VALUES ({meterId}, '{batchId}', now(), 1)";
        await ExecuteAsync(insert);

        var exception = await Should.ThrowAsync<PostgresException>(() => ExecuteAsync(insert));

        exception.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
    }

    private async Task<(int MeterId, Guid BatchId)> InsertMeterAndBatchAsync()
    {
        var location = Batches.UniqueLocation("Attic");
        var batchId = Guid.CreateVersion7();

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);

        await using var insertMeter = new NpgsqlCommand(
            "INSERT INTO meters (location, type) VALUES (@location, 'energy') RETURNING id", connection);
        insertMeter.Parameters.AddWithValue("location", location);
        var meterId = (int)(await insertMeter.ExecuteScalarAsync(CancellationToken))!;

        await using var insertBatch = new NpgsqlCommand(
            "INSERT INTO meter_batches (id, fetched_at, processed_at, reading_count) VALUES (@id, now(), now(), 1)", connection);
        insertBatch.Parameters.AddWithValue("id", batchId);
        await insertBatch.ExecuteNonQueryAsync(CancellationToken);

        return (meterId, batchId);
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(CancellationToken);
    }
}
