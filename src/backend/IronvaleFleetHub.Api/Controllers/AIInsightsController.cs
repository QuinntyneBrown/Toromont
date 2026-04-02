using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.Features.AIInsights.Commands;
using IronvaleFleetHub.Api.Features.AIInsights.Queries;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AIInsightsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AIInsightsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("predictions")]
    public async Task<ActionResult<List<AIPrediction>>> GetPredictions(
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPredictionsQuery(sort), ct);
        return Ok(result);
    }

    [HttpGet("predictions/{equipmentId:guid}")]
    public async Task<ActionResult<List<AIPrediction>>> GetPredictionsForEquipment(
        Guid equipmentId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEquipmentPredictionsQuery(equipmentId), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }

    [HttpPut("predictions/{id:guid}/dismiss")]
    public async Task<ActionResult<AIPrediction>> DismissPrediction(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DismissPredictionCommand(id), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }

    [HttpGet("anomalies")]
    public async Task<ActionResult<List<AnomalyDetection>>> GetAnomalies(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAnomaliesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("dashboard-stats")]
    public async Task<ActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAIDashboardStatsQuery(), ct);
        return Ok(new
        {
            TotalPredictions = result.TotalPredictions,
            HighPriority = result.HighPriority,
            AnomalyCount = result.AnomalyCount,
            EstimatedSavings = result.EstimatedSavings
        });
    }
}
