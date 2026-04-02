using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Features.Orders.Commands;
using IronvaleFleetHub.Api.Features.Orders.Queries;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<PartsOrder>> SubmitOrder(CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitOrderCommand(), ct);
        if (!result.IsSuccess) return BadRequest(new { Error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PartsOrder>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetOrderListQuery(skip, take), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PartsOrder>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
