using System.Diagnostics;
using MediatR;
using IronvaleFleetHub.Api.Common;
using IronvaleFleetHub.Api.Services;

namespace IronvaleFleetHub.Api.Behaviors;

public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantContext _tenantContext;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        IHttpContextAccessor httpContextAccessor,
        ITenantContext tenantContext)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = _httpContextAccessor.HttpContext?
            .Items["CorrelationId"]?.ToString() ?? "-";
        var userId = _tenantContext.UserId;
        var orgId = _tenantContext.OrganizationId;

        _logger.LogInformation(
            "[{CorrelationId}] Handling {RequestName} | User={UserId} Org={OrgId}",
            correlationId, requestName, userId, orgId);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();

            var outcome = IsFailureResult(response) ? "Failure" : "Success";
            _logger.LogInformation(
                "[{CorrelationId}] Handled {RequestName} in {ElapsedMs}ms | Outcome={Outcome}",
                correlationId, requestName, sw.ElapsedMilliseconds, outcome);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex,
                "[{CorrelationId}] Failed {RequestName} in {ElapsedMs}ms | Error={Error}",
                correlationId, requestName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private static bool IsFailureResult(TResponse response)
    {
        if (response is null) return false;
        var type = response.GetType();
        if (!type.IsGenericType) return false;
        var genericDef = type.GetGenericTypeDefinition();
        if (genericDef != typeof(Result<>)) return false;
        var isSuccessProp = type.GetProperty("IsSuccess");
        return isSuccessProp?.GetValue(response) is false;
    }
}
