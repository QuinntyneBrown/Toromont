using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IronvaleFleetHub.Api.Features.Search.Queries;

namespace IronvaleFleetHub.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<GlobalSearchResult>> Search(
        [FromQuery] string q,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return BadRequest(new { error = "Search query must be at least 2 characters." });

        var result = await _mediator.Send(new GlobalSearchQuery(q.Trim()), ct);
        return Ok(result);
    }
}
