using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Api.Tests;

public class WeatherEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WeatherEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetForecast_ReturnsOkWithFiveItems()
    {
        var response = await _client.GetAsync("/api/weather/forecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecastResponse[]>();
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
    }

    [Fact]
    public async Task GetForecastByCity_ReturnsOkWithCity()
    {
        var response = await _client.GetAsync("/api/weather/forecast/SaoPaulo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecast = await response.Content.ReadFromJsonAsync<WeatherForecastResponse>();
        Assert.NotNull(forecast);
        Assert.Equal("SaoPaulo", forecast.City);
    }

    private record WeatherForecastResponse(
        DateOnly Date,
        int TemperatureC,
        string? Summary,
        string? City,
        int TemperatureF
    );
}
