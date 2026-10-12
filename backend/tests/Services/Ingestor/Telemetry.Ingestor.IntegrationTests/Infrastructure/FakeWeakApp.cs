using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Telemetry.Ingestor.IntegrationTests.Infrastructure;

/// <summary>
/// WireMock stand-in for WeakApp that can reproduce each of its observed failure modes on demand.
/// </summary>
internal sealed class FakeWeakApp : IDisposable
{
    public const string ApiKey = "test-api-key";

    public const string ValidBody = """
        [
          {"type":"energy","name":"Kitchen","payload":{"energy":604.88}},
          {"type":"air_quality","name":"Garage","payload":{"co2":543,"pm25":26,"humidity":68}},
          {"type":"motion","name":"Office","payload":{"motionDetected":true}},
          {"type":"energy","name":"Bedroom","payload":{"energy":-5}}
        ]
        """;

    private const string Scenario = "sequence";

    private readonly WireMockServer _server = WireMockServer.Start();

    public Uri BaseUrl => new($"{_server.Url}/");

    public int MeterRequests => _server.LogEntries.Count(entry => entry.RequestMessage?.Path == "/meters");

    public static IResponseBuilder Ok(string body = ValidBody) =>
        Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(body);

    public static IResponseBuilder Status(int statusCode, string body = "error") =>
        Response.Create().WithStatusCode(statusCode).WithBody(body);

    public static IResponseBuilder RateLimited(int retryAfterSeconds) =>
        Response.Create()
            .WithStatusCode(429)
            .WithHeader("Retry-After", retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .WithBody("Too Many Requests: rate limit exceeded");

    /// <summary>Every authorised request gets the same response.</summary>
    public void Always(IResponseBuilder response) =>
        _server.Given(MetersRequest()).RespondWith(response);

    /// <summary>Authorised requests get the responses in order; the last one repeats.</summary>
    public void Sequence(params IResponseBuilder[] responses)
    {
        for (var i = 0; i < responses.Length; i++)
        {
            var mapping = _server.Given(MetersRequest()).InScenario(Scenario);
            mapping = i == 0 ? mapping : mapping.WhenStateIs(StateName(i));
            if (i < responses.Length - 1)
            {
                mapping = mapping.WillSetStateTo(StateName(i + 1));
            }

            mapping.RespondWith(responses[i]);
        }
    }

    public void Dispose() => _server.Stop();

    private static string StateName(int index) => $"step-{index}";

    private static IRequestBuilder MetersRequest() =>
        Request.Create().WithPath("/meters").UsingGet().WithHeader("X-Api-Key", ApiKey);
}
