using Npgsql;

namespace Telemetry.Gateway.IntegrationTests.Infrastructure;

/// <summary>
/// A small, fully known data set with locations unique to the test that seeds it, so tests sharing the
/// database never see each other's data. All readings lie in 10:00–11:59 UTC on <see cref="Day"/>.
/// <code>
/// Kitchen energy      10 @10:00   20 @10:10   30 @11:10
/// Kitchen air quality co2 400 / pm25 10 / hum 40 @10:00   co2 600 / pm25 20 / hum 60 @10:10
/// Garage  energy      100 @10:05
/// Garage  motion      true @10:00   false @10:10   true @10:20
/// </code>
/// </summary>
internal sealed class SeededHouse
{
    public static readonly DateTimeOffset Day = new(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);

    private SeededHouse(string suffix)
    {
        Kitchen = $"Kitchen-{suffix}";
        Garage = $"Garage-{suffix}";
    }

    public string Kitchen { get; }

    public string Garage { get; }

    public IReadOnlyList<string> Locations => [Kitchen, Garage];

    public static DateTimeOffset At(int hour, int minute) => Day.AddHours(hour).AddMinutes(minute);

    public static async Task<SeededHouse> SeedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var house = new SeededHouse(Guid.NewGuid().ToString("N")[..8]);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var kitchenEnergy = await AddMeterAsync(connection, house.Kitchen, "energy", cancellationToken);
        var kitchenAir = await AddMeterAsync(connection, house.Kitchen, "air_quality", cancellationToken);
        var garageEnergy = await AddMeterAsync(connection, house.Garage, "energy", cancellationToken);
        var garageMotion = await AddMeterAsync(connection, house.Garage, "motion", cancellationToken);

        await AddReadingAsync(connection, kitchenEnergy, At(10, 0), "energy", 10.0, cancellationToken);
        await AddReadingAsync(connection, kitchenEnergy, At(10, 10), "energy", 20.0, cancellationToken);
        await AddReadingAsync(connection, kitchenEnergy, At(11, 10), "energy", 30.0, cancellationToken);
        await AddAirQualityAsync(connection, kitchenAir, At(10, 0), (400, 10, 40), cancellationToken);
        await AddAirQualityAsync(connection, kitchenAir, At(10, 10), (600, 20, 60), cancellationToken);
        await AddReadingAsync(connection, garageEnergy, At(10, 5), "energy", 100.0, cancellationToken);
        await AddReadingAsync(connection, garageMotion, At(10, 0), "motion_detected", true, cancellationToken);
        await AddReadingAsync(connection, garageMotion, At(10, 10), "motion_detected", false, cancellationToken);
        await AddReadingAsync(connection, garageMotion, At(10, 20), "motion_detected", true, cancellationToken);

        return house;
    }

    private static async Task<int> AddMeterAsync(NpgsqlConnection connection, string location, string type, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("INSERT INTO meters (location, type) VALUES (@location, @type) RETURNING id", connection);
        command.Parameters.AddWithValue("location", location);
        command.Parameters.AddWithValue("type", type);
        return (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<Guid> AddBatchAsync(NpgsqlConnection connection, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var batchId = Guid.CreateVersion7();
        await using var command = new NpgsqlCommand(
            "INSERT INTO meter_batches (id, fetched_at, processed_at, reading_count) VALUES (@id, @at, @at, 1)", connection);
        command.Parameters.AddWithValue("id", batchId);
        command.Parameters.AddWithValue("at", at);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return batchId;
    }

    // The column name is one of the constants above, never external input.
    private static async Task AddReadingAsync(
        NpgsqlConnection connection,
        int meterId,
        DateTimeOffset at,
        string column,
        object value,
        CancellationToken cancellationToken)
    {
        var batchId = await AddBatchAsync(connection, at, cancellationToken);
        await using var command = new NpgsqlCommand(
            $"INSERT INTO readings (meter_id, batch_id, recorded_at, {column}) VALUES (@meter, @batch, @at, @value)", connection);
        command.Parameters.AddWithValue("meter", meterId);
        command.Parameters.AddWithValue("batch", batchId);
        command.Parameters.AddWithValue("at", at);
        command.Parameters.AddWithValue("value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task AddAirQualityAsync(
        NpgsqlConnection connection,
        int meterId,
        DateTimeOffset at,
        (int Co2, int Pm25, int Humidity) values,
        CancellationToken cancellationToken)
    {
        var batchId = await AddBatchAsync(connection, at, cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO readings (meter_id, batch_id, recorded_at, co2, pm25, humidity) VALUES (@meter, @batch, @at, @co2, @pm25, @humidity)",
            connection);
        command.Parameters.AddWithValue("meter", meterId);
        command.Parameters.AddWithValue("batch", batchId);
        command.Parameters.AddWithValue("at", at);
        command.Parameters.AddWithValue("co2", values.Co2);
        command.Parameters.AddWithValue("pm25", values.Pm25);
        command.Parameters.AddWithValue("humidity", values.Humidity);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
