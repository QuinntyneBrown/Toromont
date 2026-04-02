using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.DTOs;
using IronvaleFleetHub.Api.Features.Cart.Commands;
using IronvaleFleetHub.Api.Features.Cart.Queries;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<CartItem>>> GetCart(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCartQuery(), ct);
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartItem>> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddCartItemCommand(request.PartId, request.Quantity), ct);
        if (!result.IsSuccess) return BadRequest(new { Error = result.Error });
        return CreatedAtAction(nameof(GetCart), result.Value);
    }

    [HttpPut("items/{id:guid}")]
    public async Task<ActionResult<CartItem>> UpdateItem(Guid id, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCartItemCommand(id, request.Quantity), ct);
        if (!result.IsSuccess) return result.Error == "Not found." ? NotFound() : BadRequest(new { Error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new RemoveCartItemCommand(id), ct);
        if (!result.IsSuccess) return NotFound();
        return NoContent();
    }
}
