# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files
COPY AgentSapienxa.sln .
COPY src/AgentSapienxa.API/AgentSapienxa.API.csproj src/AgentSapienxa.API/
COPY src/AgentSapienxa.Application/AgentSapienxa.Application.csproj src/AgentSapienxa.Application/
COPY src/AgentSapienxa.Infrastructure/AgentSapienxa.Infrastructure.csproj src/AgentSapienxa.Infrastructure/
COPY src/AgentSapienxa.Domain/AgentSapienxa.Domain.csproj src/AgentSapienxa.Domain/
COPY tests/AgentSapienxa.IntegrationTests/AgentSapienxa.IntegrationTests.csproj tests/AgentSapienxa.IntegrationTests/
COPY tests/AgentSapienxa.UnitTests/AgentSapienxa.UnitTests.csproj tests/AgentSapienxa.UnitTests/

# Restore dependencies
RUN dotnet restore AgentSapienxa.sln

# Copy remaining source code
COPY . .

# Publish the API project
RUN dotnet publish src/AgentSapienxa.API/AgentSapienxa.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variable for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "AgentSapienxa.API.dll"]
