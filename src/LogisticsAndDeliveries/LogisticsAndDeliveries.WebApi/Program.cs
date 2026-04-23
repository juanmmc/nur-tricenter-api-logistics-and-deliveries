using LogisticsAndDeliveries.Infrastructure;
using LogisticsAndDeliveries.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using System.Security.Claims;
using System.Text.Json;

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

var keycloakSection = builder.Configuration.GetSection("Keycloak");
var authority = keycloakSection["Authority"];
var audience = keycloakSection["Audience"];
var clientId = keycloakSection["ClientId"];
var requireHttpsMetadata = bool.TryParse(keycloakSection["RequireHttpsMetadata"], out var parsedRequireHttps)
    ? parsedRequireHttps
    : false;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = requireHttpsMetadata;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity)
                {
                    return Task.CompletedTask;
                }

                AddRealmRoles(identity);
                AddClientRoles(identity, clientId);

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrDriver", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin", "driver");
    });
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapMetrics();

app.Run();

static void AddRealmRoles(ClaimsIdentity identity)
{
    var realmAccessClaim = identity.FindFirst("realm_access")?.Value;
    if (string.IsNullOrWhiteSpace(realmAccessClaim))
    {
        return;
    }

    using var realmDoc = JsonDocument.Parse(realmAccessClaim);
    if (!realmDoc.RootElement.TryGetProperty("roles", out var rolesElement) || rolesElement.ValueKind != JsonValueKind.Array)
    {
        return;
    }

    foreach (var roleElement in rolesElement.EnumerateArray())
    {
        var role = roleElement.GetString();
        if (string.IsNullOrWhiteSpace(role))
        {
            continue;
        }

        if (!identity.HasClaim(ClaimTypes.Role, role))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
    }
}

static void AddClientRoles(ClaimsIdentity identity, string? clientId)
{
    if (string.IsNullOrWhiteSpace(clientId))
    {
        return;
    }

    var resourceAccessClaim = identity.FindFirst("resource_access")?.Value;
    if (string.IsNullOrWhiteSpace(resourceAccessClaim))
    {
        return;
    }

    using var resourceDoc = JsonDocument.Parse(resourceAccessClaim);
    if (!resourceDoc.RootElement.TryGetProperty(clientId, out var clientElement))
    {
        return;
    }

    if (!clientElement.TryGetProperty("roles", out var rolesElement) || rolesElement.ValueKind != JsonValueKind.Array)
    {
        return;
    }

    foreach (var roleElement in rolesElement.EnumerateArray())
    {
        var role = roleElement.GetString();
        if (string.IsNullOrWhiteSpace(role))
        {
            continue;
        }

        if (!identity.HasClaim(ClaimTypes.Role, role))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
    }
}

public partial class Program
{
}