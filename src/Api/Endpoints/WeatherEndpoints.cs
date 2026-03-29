namespace Api.Endpoints;

public static class WeatherEndpoints
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public static void MapWeatherEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/weather")
            .WithTags("Weather");

        group.MapGet("/forecast", () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            return Results.Ok(forecast);
        })
        .WithName("GetWeatherForecast")
        .WithSummary("Retorna previsão do tempo para os próximos 5 dias")
        .Produces<WeatherForecast[]>(200);

        group.MapGet("/forecast/{city}", (string city) =>
        {
            var forecast = new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                City = city
            };

            return Results.Ok(forecast);
        })
        .WithName("GetWeatherByCity")
        .WithSummary("Retorna previsão do tempo para uma cidade específica")
        .Produces<WeatherForecast>(200);
    }
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public string? City { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
