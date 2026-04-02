using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AIInsightsController : ControllerBase
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<AIInsightsController> _logger;

    public AIInsightsController(FleetHubDbContext db, ITenantContext tenant, ILogger<AIInsightsController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet("predictions")]
    public async Task<ActionResult<List<AIPrediction>>> GetPredictions(
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        var query = _db.AIPredictions
            .Include(p => p.Equipment)
            .Where(p => p.OrganizationId == _tenant.OrganizationId && !p.IsDismissed)
            .AsNoTracking();

        query = sort?.ToLowerInvariant() switch
        {
            "confidence" => query.OrderByDescending(p => p.ConfidenceScore),
            "-confidence" => query.OrderBy(p => p.ConfidenceScore),
            "priority" => query.OrderByDescending(p => p.Priority == "Critical" ? 0 :
                                                        p.Priority == "High" ? 1 :
                                                        p.Priority == "Medium" ? 2 : 3),
            _ => query.OrderByDescending(p => p.ConfidenceScore)
        };

        var predictions = await query.ToListAsync(ct);
        return Ok(predictions);
    }

    [HttpGet("predictions/{equipmentId:guid}")]
    public async Task<ActionResult<List<AIPrediction>>> GetPredictionsForEquipment(
        Guid equipmentId,
        CancellationToken ct)
    {
        var equipmentExists = await _db.Equipment
            .AnyAsync(e => e.Id == equipmentId && e.OrganizationId == _tenant.OrganizationId, ct);

        if (!equipmentExists)
            return NotFound();

        var predictions = await _db.AIPredictions
            .Where(p => p.EquipmentId == equipmentId && !p.IsDismissed)
            .OrderByDescending(p => p.ConfidenceScore)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(predictions);
    }

    [HttpPut("predictions/{id:guid}/dismiss")]
    public async Task<ActionResult<AIPrediction>> DismissPrediction(Guid id, CancellationToken ct)
    {
        var prediction = await _db.AIPredictions
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == _tenant.OrganizationId, ct);

        if (prediction is null)
            return NotFound();

        prediction.IsDismissed = true;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Prediction {PredictionId} dismissed by user {UserId}", id, _tenant.UserId);
        return Ok(prediction);
    }

    [HttpGet("anomalies")]
    public async Task<ActionResult<List<AnomalyDetection>>> GetAnomalies(CancellationToken ct)
    {
        var anomalies = await _db.AnomalyDetections
            .Include(a => a.Equipment)
            .Where(a => a.OrganizationId == _tenant.OrganizationId)
            .OrderByDescending(a => a.DetectedAt)
            .Take(100)
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(anomalies);
    }

    [HttpGet("dashboard-stats")]
    public async Task<ActionResult> GetDashboardStats(CancellationToken ct)
    {
        var orgId = _tenant.OrganizationId;

        var totalPredictions = await _db.AIPredictions
            .CountAsync(p => p.OrganizationId == orgId && !p.IsDismissed, ct);

        var highPriority = await _db.AIPredictions
            .CountAsync(p => p.OrganizationId == orgId && !p.IsDismissed
                && (p.Priority == "Critical" || p.Priority == "High"), ct);

        var anomalyCount = await _db.AnomalyDetections
            .CountAsync(a => a.OrganizationId == orgId
                && a.DetectedAt >= DateTime.UtcNow.AddDays(-30), ct);

        var estimatedSavings = totalPredictions * 2500m; // Estimated savings per prediction acted upon

        return Ok(new
        {
            TotalPredictions = totalPredictions,
            HighPriority = highPriority,
            AnomalyCount = anomalyCount,
            EstimatedSavings = estimatedSavings
        });
    }
}
