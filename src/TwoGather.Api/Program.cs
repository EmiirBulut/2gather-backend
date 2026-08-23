using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using TwoGather.Api.Hubs;
using TwoGather.Api.Middleware;
using TwoGather.Application;
using TwoGather.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Render/Docker inject PORT at runtime; bind to it when present. Local dev keeps
// using the URLs from launchSettings.json (PORT is never set there).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Application & Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// SignalR
builder.Services.AddSignalR();

// Notification service (generic hub binding in Api layer to avoid circular dependency)
builder.Services.AddScoped<TwoGather.Application.Common.Interfaces.INotificationService,
    TwoGather.Infrastructure.Services.SignalRNotificationService<ListHub>>();

// JWT Authentication
const string jwtSecretPlaceholder = "PLACEHOLDER_REPLACE_WITH_STRONG_SECRET_KEY_MIN_32_CHARS";
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (builder.Environment.IsProduction() &&
    (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret == jwtSecretPlaceholder))
{
    throw new InvalidOperationException(
        "Jwt:Secret is missing or still set to the placeholder value. " +
        "Set a real secret via the Jwt__Secret environment variable before running in Production.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // SignalR: read token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS — allowed origins come from config (Cors:AllowedOrigins, comma-separated).
// Local dev default lives in appsettings.json; production origin(s) are supplied
// via the Cors__AllowedOrigins environment variable once the frontend domain exists.
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// OpenAPI spec
builder.Services.AddOpenApi();

// Health check — plain liveness probe for Render, no DB dependency.
builder.Services.AddHealthChecks();

var app = builder.Build();

// Render terminates TLS at its edge and forwards plain HTTP to the container.
// Trust X-Forwarded-* so downstream scheme/IP detection (UseHttpsRedirection, etc.)
// is correct. Proxy IPs aren't static on Render, so known network/proxy lists are cleared.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "2gather API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ListHub>("/hubs/list");
app.MapHealthChecks("/health");

app.Run();
