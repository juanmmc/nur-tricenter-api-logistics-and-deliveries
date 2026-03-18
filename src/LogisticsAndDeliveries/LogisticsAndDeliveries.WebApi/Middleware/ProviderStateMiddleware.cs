using LogisticsAndDeliveries.Domain.Drivers;
using LogisticsAndDeliveries.Infrastructure.Persistence.PersistenceModel;
using LogisticsAndDeliveries.Infrastructure.Persistence.PersistenceModel.EFCoreEntities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace LogisticsAndDeliveries.WebApi.Middleware
{
    public class ProviderStateMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;

        public ProviderStateMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/provider-states"))
            {
                await HandleProviderStatesRequest(context);
            }
            else
            {
                await _next(context);
            }
        }

        private async Task HandleProviderStatesRequest(HttpContext context)
        {
            context.Response.StatusCode = 200;

            if (context.Request.Method == HttpMethod.Post.Method)
            {
                string jsonRequestBody;
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    jsonRequestBody = await reader.ReadToEndAsync();
                }

                var providerState = JsonSerializer.Deserialize<ProviderState>(
                    jsonRequestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (providerState?.State != null)
                {
                    await SetupProviderState(providerState.State, providerState.Params);
                }
            }

            await context.Response.WriteAsync(string.Empty);
        }

        private async Task SetupProviderState(string state, Dictionary<string, object>? parameters)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PersistenceDbContext>();

            switch (state)
            {
                case "A driver with ID 3fa85f64-5717-4562-b3fc-2c963f66afa6 exists":
                    var existingDriver = await dbContext.Driver
                        .FirstOrDefaultAsync(d => d.Id == Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));

                    if (existingDriver == null)
                    {
                        var driver = new DriverPersistenceModel
                        {
                            Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                            Name = "Juan Pérez",
                            Latitude = 40.4168,
                            Longitude = -3.7038,
                            LastLocationUpdate = DateTime.UtcNow
                        };
                        dbContext.Driver.Add(driver);
                        await dbContext.SaveChangesAsync();
                    }
                    break;

                case "No driver with ID 00000000-0000-0000-0000-000000000000 exists":
                    var driverToRemove = await dbContext.Driver
                        .FirstOrDefaultAsync(d => d.Id == Guid.Parse("00000000-0000-0000-0000-000000000000"));

                    if (driverToRemove != null)
                    {
                        dbContext.Driver.Remove(driverToRemove);
                        await dbContext.SaveChangesAsync();
                    }
                    break;

                case "Multiple drivers exist in the system":
                    // Limpiar y agregar múltiples drivers
                    var drivers = new List<DriverPersistenceModel>
                    {
                        new DriverPersistenceModel
                        {
                            Id = Guid.NewGuid(),
                            Name = "Juan Pérez",
                            Latitude = 40.4168,
                            Longitude = -3.7038,
                            LastLocationUpdate = DateTime.UtcNow
                        },
                        new DriverPersistenceModel
                        {
                            Id = Guid.NewGuid(),
                            Name = "María García",
                            Latitude = 41.3851,
                            Longitude = 2.1734,
                            LastLocationUpdate = DateTime.UtcNow
                        }
                    };

                    foreach (var driver in drivers)
                    {
                        var exists = await dbContext.Driver.AnyAsync(d => d.Id == driver.Id);
                        if (!exists)
                        {
                            dbContext.Driver.Add(driver);
                        }
                    }
                    await dbContext.SaveChangesAsync();
                    break;

                case "Valid driver data is provided":
                    // No se necesita setup especial, solo asegurar BD limpia
                    var newDriverId = Guid.Parse("4fa85f64-5717-4562-b3fc-2c963f66afa7");
                    var conflictingDriver = await dbContext.Driver.FirstOrDefaultAsync(d => d.Id == newDriverId);
                    if (conflictingDriver != null)
                    {
                        dbContext.Driver.Remove(conflictingDriver);
                        await dbContext.SaveChangesAsync();
                    }
                    break;

                case "Driver name is empty or invalid":
                    // No se necesita setup especial
                    break;

                default:
                    break;
            }
        }

        private class ProviderState
        {
            public string? State
            {
                get; set;
            }
            public Dictionary<string, object>? Params
            {
                get; set;
            }
        }
    }

    public static class ProviderStateMiddlewareExtensions
    {
        public static IApplicationBuilder UseProviderState(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProviderStateMiddleware>();
        }
    }
}