namespace Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/health")
            .WithTags("Health");

        group.MapGet("/", (ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("HealthEndpoints");
            logger.LogInformation("Health check requested");

            return Results.Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "1.0.0"
            });
        })
        .WithName("HealthCheck")
        .WithSummary("Verifica se a API está saudável")
        .Produces(200);

        group.MapGet("/ready", (ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("HealthEndpoints");
            logger.LogInformation("Readiness check requested");

            return Results.Ok(new
            {
                Ready = true,
                Timestamp = DateTime.UtcNow
            });
        })
        .WithName("ReadinessCheck")
        .WithSummary("Verifica se a API está pronta para receber tráfego")
        .Produces(200);
    }
}
