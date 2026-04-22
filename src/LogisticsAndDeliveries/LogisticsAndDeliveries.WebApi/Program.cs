using LogisticsAndDeliveries.Infrastructure;
using LogisticsAndDeliveries.WebApi.Middleware;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configurar para aceptar camelCase en JSON
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Permitir conversión case-insensitive
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddInfrastructure(builder.Configuration);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await app.Services.ApplyMigrationsAsync();

// Middleware para Provider States (en testing y desarrollo)
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Testing")
{
    app.UseProviderState();
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpMetrics();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapMetrics();

app.Run();

public partial class Program
{
}