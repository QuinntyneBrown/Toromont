using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Hubs;
using ToromontFleetHub.Api.Middleware;
using ToromontFleetHub.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ToromontFleetHub")
        .WriteTo.Console();

    var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        loggerConfig.WriteTo.ApplicationInsights(
            appInsightsConnectionString,
            TelemetryConverter.Traces);
    }
});

// --- EF Core ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<FleetHubDbContext>((sp, options) =>
    {
        var tenantContext = sp.GetService<ITenantContext>();
        options.UseInMemoryDatabase("FleetHubDev");
    });
}
else
{
    builder.Services.AddDbContext<FleetHubDbContext>((sp, options) =>
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(3);
        });
    });
}

// --- Tenant Context ---
builder.Services.AddScoped<ITenantContext, TenantContext>();

// --- MediatR ---
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// --- FluentValidation ---
builder.Services.AddFluentValidationAutoValidation();

// --- Authentication ---
var useDevAuth = builder.Configuration.GetValue<bool>("Authentication:UseDevMode");
if (useDevAuth)
{
    builder.Services.AddAuthentication("DevScheme")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevAuthHandler>("DevScheme", null);
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["AzureAd:Instance"] + builder.Configuration["AzureAd:TenantId"];
            options.Audience = builder.Configuration["AzureAd:ClientId"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["AzureAd:Instance"] + builder.Configuration["AzureAd:TenantId"] + "/v2.0",
            };
        });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireAdminOrFleetManager", policy => policy.RequireRole("Admin", "FleetManager"));
    options.AddPolicy("RequireFleetManager", policy => policy.RequireRole("Admin", "FleetManager"));
    options.AddPolicy("RequireWrite", policy => policy.RequireRole("Admin", "FleetManager", "Technician"));
    options.AddPolicy("RequireRead", policy => policy.RequireAuthenticatedUser());
});

// --- SignalR ---
builder.Services.AddSignalR();

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- Rate Limiting ---
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Toromont Fleet Hub API",
        Version = "v1",
        Description = "Fleet management, work orders, parts ordering, and AI-powered predictive maintenance.",
    });
});

// --- Application Services ---
builder.Services.AddScoped<IAlertEvaluatorService, AlertEvaluatorService>();
builder.Services.AddScoped<IWorkOrderNumberGenerator, WorkOrderNumberGenerator>();
builder.Services.AddScoped<IReportGenerationService, ReportGenerationService>();
builder.Services.AddScoped<IExportService, ExportService>();

// --- Controllers ---
builder.Services.AddControllers();

// --- Application Insights ---
var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = aiConnectionString;
    });
}

var app = builder.Build();

// --- Middleware pipeline ---

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Toromont Fleet Hub API v1"));
}

app.UseSerilogRequestLogging();
app.UseCors("AngularDev");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();

app.MapControllers().RequireAuthorization();
app.MapHub<NotificationHub>("/hubs/notifications");

// --- Seed data in development ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FleetHubDbContext>();
    await DataSeeder.SeedAsync(db);
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }

// Dev authentication handler that always succeeds
public class DevAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    public DevAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "dev-user-1"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "admin@toromont.com"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Dev Admin"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin"),
            new System.Security.Claims.Claim("organizationId", "a1b2c3d4-0001-0000-0000-000000000001"),
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, Scheme.Name);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}
