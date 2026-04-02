using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToromontFleetHub.Api.DTOs;
using ToromontFleetHub.Api.Features.Equipment.Commands;
using ToromontFleetHub.Api.Features.Equipment.Queries;

namespace ToromontFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/equipment")]
[Authorize]
public class EquipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public EquipmentController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<Models.Equipment>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? filter = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetEquipmentListQuery(skip, take, status, category, search, sort, filter), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Models.Equipment>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEquipmentByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminOrFleetManager")]
    public async Task<ActionResult<Models.Equipment>> Create(
        [FromBody] CreateEquipmentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess) return BadRequest(new { Error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Models.Equipment>> Update(
        Guid id, [FromBody] UpdateEquipmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateEquipmentCommand(id, request.Name, request.Status, request.Location,
                request.Latitude, request.Longitude, request.GpsDeviceId, request.Notes), ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdminOrFleetManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteEquipmentCommand(id), ct);
        if (!result.IsSuccess) return NotFound();
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Policy = "RequireAdminOrFleetManager")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { Error = "No file uploaded." });

        if (!file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { Error = "Only XML files are accepted." });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { Error = "File exceeds maximum size of 10MB." });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportEquipmentCommand(stream), ct);
        return Ok(new { Imported = result.Imported });
    }
}
