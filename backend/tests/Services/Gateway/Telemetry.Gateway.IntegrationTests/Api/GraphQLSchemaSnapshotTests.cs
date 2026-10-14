using HotChocolate.Execution;
using Telemetry.Gateway.IntegrationTests.Infrastructure;

namespace Telemetry.Gateway.IntegrationTests.Api;

/// <summary>
/// Keeps <c>schema.graphql</c> (next to the gateway project, used by the frontend's code generation) in
/// sync with the running schema, so every schema change is reviewed as a diff.
/// Set <c>UPDATE_GRAPHQL_SCHEMA=1</c> to rewrite the file after an intended change.
/// </summary>
public sealed class GraphQLSchemaSnapshotTests(GatewayDatabaseFixture database)
{
    private const string UpdateVariable = "UPDATE_GRAPHQL_SCHEMA";

    [Fact]
    public async Task Schema_MatchesCommittedSnapshot()
    {
        await using var factory = new GatewayApiFactory(database);
        var executor = await factory.Services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var actual = Normalize(executor.Schema.ToString());

        var snapshotPath = FindSnapshotPath();
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(snapshotPath, actual, TestContext.Current.CancellationToken);
        }

        File.Exists(snapshotPath).ShouldBeTrue($"{snapshotPath} is missing; run the tests with {UpdateVariable}=1 to create it.");
        var expected = Normalize(await File.ReadAllTextAsync(snapshotPath, TestContext.Current.CancellationToken));
        actual.ShouldBe(expected, $"The GraphQL schema changed. Review the change and run the tests with {UpdateVariable}=1 to accept it.");
    }

    private static string Normalize(string sdl) => sdl.ReplaceLineEndings("\n").TrimEnd() + "\n";

    private static string FindSnapshotPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Telemetry.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("The backend solution directory was not found.");
        return Path.Combine(directory.FullName, "src", "Services", "Gateway", "Telemetry.Gateway.Api", "schema.graphql");
    }
}
