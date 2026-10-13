using Telemetry.Processor.IntegrationTests.Infrastructure;
using Telemetry.Testing.RabbitMq;

[assembly: AssemblyFixture(typeof(MigratedDatabaseFixture))]
[assembly: AssemblyFixture(typeof(RabbitMqFixture))]
