# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files
COPY AgentSapienxa.sln .
COPY src/AgentSapienxa.API/AgentSapienxa.API.csproj src/AgentSapienxa.API/
COPY src/AgentSapienxa.Application/AgentSapienxa.Application.csproj src/AgentSapienxa.Application/
COPY src/AgentSapienxa.Infrastructure/AgentSapienxa.Infrastructure.csproj src/AgentSapienxa.Infrastructure/
COPY src/AgentSapienxa.Domain/AgentSapienxa.Domain.csproj src/AgentSapienxa.Domain/
# COPY tests/AgentSapienxa.IntegrationTests/AgentSapienxa.IntegrationTests.csproj tests/AgentSapienxa.IntegrationTests/
# COPY tests/AgentSapienxa.UnitTests/AgentSapienxa.UnitTests.csproj tests/AgentSapienxa.UnitTests/

# Restore dependencies
RUN dotnet restore src/AgentSapienxa.API/AgentSapienxa.API.csproj

# Copy remaining source code
COPY . .

# Publish the API project
RUN dotnet publish src/AgentSapienxa.API/AgentSapienxa.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Instalar curl para health check
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*


# Copy published application from build stage
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=5 \
    CMD curl -f http://localhost:8080/ || exit 1

# Set environment variable for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "AgentSapienxa.API.dll"]
