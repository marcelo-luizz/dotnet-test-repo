namespace Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/health")
            .WithTags("Health");

        group.MapGet("/", () => Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "1.0.0"
        }))
        .WithName("HealthCheck")
        .WithSummary("Verifica se a API está saudável")
        .Produces(200);

        group.MapGet("/ready", () => Results.Ok(new
        {
            Ready = true,
            Timestamp = DateTime.UtcNow
        }))
        .WithName("ReadinessCheck")
        .WithSummary("Verifica se a API está pronta para receber tráfego")
        .Produces(200);
    }
}
