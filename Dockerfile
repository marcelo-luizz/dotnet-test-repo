FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY SampleApi.sln ./
COPY src/Api/Api.csproj ./src/Api/
COPY tests/Api.Tests/Api.Tests.csproj ./tests/Api.Tests/

RUN dotnet restore

COPY . .

RUN dotnet test --no-restore --verbosity normal

RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

USER appuser

ENTRYPOINT ["dotnet", "Api.dll"]
