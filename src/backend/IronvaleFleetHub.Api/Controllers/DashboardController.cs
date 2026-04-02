using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Features.Dashboard.Queries;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("kpis")]
    public async Task<ActionResult<DashboardStats>> GetKpis(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardKpisQuery(), ct);
        return Ok(result);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<List<Alert>>> GetRecentAlerts(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardAlertsQuery(), ct);
        return Ok(result);
    }
}
