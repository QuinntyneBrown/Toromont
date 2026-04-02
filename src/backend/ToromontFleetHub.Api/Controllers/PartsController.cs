using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Features.Parts.Queries;
using ToromontFleetHub.Api.Models;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/parts")]
[Authorize]
public class PartsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PartsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<Part>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? compatibility = null,
        [FromQuery] string? availability = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetPartsListQuery(skip, take, category, compatibility, availability, search, sort), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Part>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPartByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Part>>> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { Error = "Query parameter 'q' is required." });

        var result = await _mediator.Send(new SearchPartsQuery(q), ct);
        return Ok(result);
    }
}
