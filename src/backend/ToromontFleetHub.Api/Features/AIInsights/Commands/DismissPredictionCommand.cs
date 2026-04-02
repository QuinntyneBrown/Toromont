using MediatR;
using Microsoft.EntityFrameworkCore;
using ToromontFleetHub.Api.Common;
using ToromontFleetHub.Api.Data;
using ToromontFleetHub.Api.Models;
using ToromontFleetHub.Api.Services;

namespace ToromontFleetHub.Api.Features.AIInsights.Commands;

public record DismissPredictionCommand(Guid Id) : IRequest<Result<AIPrediction>>;

public class DismissPredictionCommandHandler : IRequestHandler<DismissPredictionCommand, Result<AIPrediction>>
{
    private readonly FleetHubDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<DismissPredictionCommandHandler> _logger;

    public DismissPredictionCommandHandler(
        FleetHubDbContext db, ITenantContext tenant,
        ILogger<DismissPredictionCommandHandler> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Result<AIPrediction>> Handle(DismissPredictionCommand request, CancellationToken ct)
    {
        var prediction = await _db.AIPredictions
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.OrganizationId == _tenant.OrganizationId, ct);

        if (prediction is null)
            return Result<AIPrediction>.Failure("Not found.");

        prediction.IsDismissed = true;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Prediction {PredictionId} dismissed by user {UserId}", request.Id, _tenant.UserId);
        return Result<AIPrediction>.Success(prediction);
    }
}
