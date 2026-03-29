# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files first (for layer caching)
COPY SampleApi.sln ./
COPY src/Api/Api.csproj ./src/Api/
COPY tests/Api.Tests/Api.Tests.csproj ./tests/Api.Tests/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Run tests
RUN dotnet test --no-restore --verbosity normal

# Publish
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser

COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

USER appuser

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "Api.dll"]
