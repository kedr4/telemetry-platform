using System.Text.Json.Serialization.Metadata;
using Telemetry.Gateway.Api.Errors;
using Telemetry.Gateway.Api.GraphQL;
using Telemetry.Gateway.Api.Json;
using Telemetry.Gateway.Api.Rest;

namespace Telemetry.Gateway.Api;

public static class ApiExtensions
{
    public const string CorsPolicyName = "frontend";
    public const string GraphQLPath = "/graphql";
    public const string OpenApiDocumentPath = "/openapi/v1.json";

    public static WebApplicationBuilder AddGatewayApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolver =
                (options.SerializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
                .WithAddedModifier(ReadingJsonPolymorphism.Apply));

        services.AddExceptionHandler<RequestValidationExceptionHandler>();
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "Telemetry Gateway REST API";
            document.Info.Description = "Meters, readings and aggregations of the smart-home telemetry platform. "
                + $"The same data is available through GraphQL at {GraphQLPath}.";
            return Task.CompletedTask;
        }));

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .WithMethods("GET", "POST")));

        services.AddTelemetryGraphQL(builder.Environment);

        return builder;
    }

    public static WebApplication UseGatewayApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseCors(CorsPolicyName);

        app.MapOpenApi(OpenApiDocumentPath);
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(OpenApiDocumentPath, "Telemetry Gateway v1");
            options.DocumentTitle = "Telemetry Gateway REST API";
        });

        // Nitro (the GraphQL IDE) is served on GET /graphql from a browser.
        app.MapGraphQL(GraphQLPath);
        app.MapRestEndpoints();

        return app;
    }
}
