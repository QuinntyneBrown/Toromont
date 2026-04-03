using Microsoft.EntityFrameworkCore;
using IronvaleFleetHub.Api.Data;

namespace IronvaleFleetHub.Api.Services.AI;

public class DevNaturalLanguageSearchService : IAiSearchService
{
    private readonly FleetHubDbContext _db;
    private readonly TokenVectorizer _vectorizer;
    private readonly ILogger<DevNaturalLanguageSearchService> _logger;

    public DevNaturalLanguageSearchService(
        FleetHubDbContext db,
        TokenVectorizer vectorizer,
        ILogger<DevNaturalLanguageSearchService> logger)
    {
        _db = db;
        _vectorizer = vectorizer;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AiSearchResult>> SearchPartsAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<AiSearchResult>();

        var queryTokens = _vectorizer.Tokenize(query);
        _logger.LogDebug("Search query tokenized to {TokenCount} tokens: {Tokens}",
            queryTokens.Count, string.Join(", ", queryTokens.Keys.Take(10)));

        var parts = await _db.Parts.AsNoTracking().ToListAsync(ct);

        var scored = parts.Select(part =>
        {
            var docText = $"{part.Name} {part.Description} {part.Category} {part.CompatibleModels}";
            var docTokens = _vectorizer.Tokenize(docText);
            var score = _vectorizer.CalculateSimilarity(queryTokens, docTokens);

            var matchReasons = new List<string>();
            foreach (var qt in queryTokens.Keys)
            {
                if (docTokens.ContainsKey(qt))
                    matchReasons.Add(queryTokens[qt] < 1.0 ? $"synonym: {qt}" : $"direct: {qt}");
            }

            return new AiSearchResult
            {
                PartId = part.Id,
                PartNumber = part.PartNumber,
                Description = part.Name,
                Score = score,
                MatchReason = matchReasons.Count > 0
                    ? string.Join(", ", matchReasons.Take(5))
                    : "no match"
            };
        })
        .Where(r => r.Score > 0)
        .OrderByDescending(r => r.Score)
        .Take(20)
        .ToList();

        _logger.LogDebug("Search returned {Count} results for query '{Query}'", scored.Count, query);
        return scored;
    }
}
