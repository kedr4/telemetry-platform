using Telemetry.Gateway.Api.GraphQL.Types;

namespace Telemetry.Gateway.Api.GraphQL;

internal static class GraphQLServiceCollectionExtensions
{
    public static IServiceCollection AddTelemetryGraphQL(this IServiceCollection services, IHostEnvironment environment) =>
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddType<ReadingInterfaceType>()
            .AddType<EnergyReadingType>()
            .AddType<AirQualityReadingType>()
            .AddType<MotionReadingType>()
            .AddType<MeterObjectType>()
            .AddType<AggregateResultType>()
            .AddErrorFilter<ValidationErrorFilter>()
            .ModifyRequestOptions(options => options.IncludeExceptionDetails = environment.IsDevelopment())
            .Services;
}
