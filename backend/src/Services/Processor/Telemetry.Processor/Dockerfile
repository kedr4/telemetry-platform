# syntax=docker/dockerfile:1
# Build context: backend/

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached until a project file or package version changes).
COPY global.json nuget.config Directory.Build.props Directory.Packages.props stylecop.json .editorconfig ./
COPY src/BuildingBlocks/Telemetry.Contracts/Telemetry.Contracts.csproj src/BuildingBlocks/Telemetry.Contracts/
COPY src/BuildingBlocks/Telemetry.BuildingBlocks/Telemetry.BuildingBlocks.csproj src/BuildingBlocks/Telemetry.BuildingBlocks/
COPY src/Services/Processor/Telemetry.Processor/Telemetry.Processor.csproj src/Services/Processor/Telemetry.Processor/
RUN dotnet restore src/Services/Processor/Telemetry.Processor/Telemetry.Processor.csproj

COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Processor/ src/Services/Processor/
RUN dotnet publish src/Services/Processor/Telemetry.Processor/Telemetry.Processor.csproj \
    --configuration Release --no-restore --output /app -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Run as the non-root user shipped with the .NET images.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Telemetry.Processor.dll"]
