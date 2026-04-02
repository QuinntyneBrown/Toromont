using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Features.Reports.Commands;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("fleet-utilization")]
    public async Task<ActionResult<ReportResponse>> FleetUtilization(
        [FromBody] ReportRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateFleetUtilizationReportCommand(request), ct);
        return Ok(result);
    }

    [HttpPost("maintenance-costs")]
    public async Task<ActionResult<ReportResponse>> MaintenanceCosts(
        [FromBody] ReportRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateMaintenanceCostsReportCommand(request), ct);
        return Ok(result);
    }

    [HttpPost("{type}/export")]
    public async Task<IActionResult> Export(
        string type,
        [FromBody] ReportRequest request,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ExportReportCommand(type, request, format), ct);
        if (!result.IsSuccess) return BadRequest(new { Error = result.Error });
        return File(result.Value!.FileBytes, result.Value.ContentType, result.Value.FileName);
    }
}
