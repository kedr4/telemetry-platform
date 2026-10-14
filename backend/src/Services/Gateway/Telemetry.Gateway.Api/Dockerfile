# syntax=docker/dockerfile:1
# Build context: backend/

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached until a project file or package version changes).
COPY global.json nuget.config Directory.Build.props Directory.Packages.props stylecop.json .editorconfig ./
COPY src/BuildingBlocks/Telemetry.Contracts/Telemetry.Contracts.csproj src/BuildingBlocks/Telemetry.Contracts/
COPY src/BuildingBlocks/Telemetry.BuildingBlocks/Telemetry.BuildingBlocks.csproj src/BuildingBlocks/Telemetry.BuildingBlocks/
COPY src/Services/Gateway/Telemetry.Gateway.Domain/Telemetry.Gateway.Domain.csproj src/Services/Gateway/Telemetry.Gateway.Domain/
COPY src/Services/Gateway/Telemetry.Gateway.Application/Telemetry.Gateway.Application.csproj src/Services/Gateway/Telemetry.Gateway.Application/
COPY src/Services/Gateway/Telemetry.Gateway.Infrastructure/Telemetry.Gateway.Infrastructure.csproj src/Services/Gateway/Telemetry.Gateway.Infrastructure/
COPY src/Services/Gateway/Telemetry.Gateway.Api/Telemetry.Gateway.Api.csproj src/Services/Gateway/Telemetry.Gateway.Api/
RUN dotnet restore src/Services/Gateway/Telemetry.Gateway.Api/Telemetry.Gateway.Api.csproj

COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Gateway/ src/Services/Gateway/
RUN dotnet publish src/Services/Gateway/Telemetry.Gateway.Api/Telemetry.Gateway.Api.csproj \
    --configuration Release --no-restore --output /app -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Run as the non-root user shipped with the .NET images.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Telemetry.Gateway.Api.dll"]
