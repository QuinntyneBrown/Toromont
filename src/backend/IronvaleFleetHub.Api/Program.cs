using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using IronvaleFleetHub.Api.Behaviors;
using IronvaleFleetHub.Api.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using IronvaleFleetHub.Api.Authentication;
using IronvaleFleetHub.Api.Data;
using IronvaleFleetHub.Api.Hubs;
using IronvaleFleetHub.Api.Middleware;
using IronvaleFleetHub.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "IronvaleFleetHub")
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
builder.Services.AddSingleton<IUserBlacklist, InMemoryUserBlacklist>();

// --- MediatR ---
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// --- FluentValidation ---
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
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
        Title = "Ironvale Fleet Hub API",
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
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ironvale Fleet Hub API v1"));
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
