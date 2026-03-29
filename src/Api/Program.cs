using Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Sample API",
        Version = "v1",
        Description = "API simples para testar pipeline CI/CD"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthEndpoints();
app.MapWeatherEndpoints();

app.Run();

public partial class Program { }
