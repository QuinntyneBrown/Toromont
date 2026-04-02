using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Features.WorkOrders.Commands;
using ToromontFleetHub.Api.Features.WorkOrders.Queries;
using ToromontFleetHub.Api.Models;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/work-orders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<WorkOrder>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? sort = null,
        [FromQuery] Guid? equipmentId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetWorkOrderListQuery(skip, take, status, sort, equipmentId), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkOrder>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWorkOrderByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrder>> Create([FromBody] CreateWorkOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateWorkOrderCommand(
            request.EquipmentId,
            request.ServiceType,
            request.Priority,
            request.Description,
            request.RequestedDate,
            request.AssignedToUserId), ct);
        if (!result.IsSuccess) return BadRequest(new { Error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<WorkOrder>> UpdateStatus(
        Guid id, [FromBody] StatusUpdateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateWorkOrderStatusCommand(id, request.Status, request.Notes), ct);
        if (!result.IsSuccess) return result.Error == "Not found." ? NotFound() : BadRequest(new { Error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("calendar")]
    public async Task<ActionResult> GetCalendar(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWorkOrderCalendarQuery(start, end), ct);
        return Ok(result);
    }
}
