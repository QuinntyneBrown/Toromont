namespace IronvaleFleetHub.Api.Services.AI;

public interface IAiSearchService
{
    Task<IReadOnlyList<AiSearchResult>> SearchPartsAsync(string query, CancellationToken ct = default);
}

public class AiSearchResult
{
    public Guid PartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string MatchReason { get; set; } = string.Empty;
}
