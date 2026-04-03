namespace IronvaleFleetHub.Api.Services.AI;

public class TokenVectorizer
{
    private readonly SynonymCatalog _catalog;

    public TokenVectorizer(SynonymCatalog catalog) => _catalog = catalog;

    public Dictionary<string, double> Tokenize(string text)
    {
        var tokens = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        var words = Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            // Direct token
            AddOrIncrement(tokens, word, 1.0);

            // Synonym expansion
            foreach (var expanded in _catalog.Expand(word))
            {
                if (!expanded.Equals(word, StringComparison.OrdinalIgnoreCase))
                    AddOrIncrement(tokens, expanded, 0.7);
            }
        }

        // Bigrams for multi-word synonym matching
        for (int i = 0; i < words.Length - 1; i++)
        {
            var bigram = $"{words[i]} {words[i + 1]}";
            foreach (var expanded in _catalog.Expand(bigram))
            {
                AddOrIncrement(tokens, expanded, 0.9);
            }
        }

        return tokens;
    }

    public decimal CalculateSimilarity(Dictionary<string, double> queryTokens, Dictionary<string, double> docTokens)
    {
        if (queryTokens.Count == 0 || docTokens.Count == 0) return 0m;

        double dotProduct = 0;
        double queryMag = 0;
        double docMag = 0;

        foreach (var kvp in queryTokens)
        {
            queryMag += kvp.Value * kvp.Value;
            if (docTokens.TryGetValue(kvp.Key, out var docValue))
                dotProduct += kvp.Value * docValue;
        }

        foreach (var kvp in docTokens)
            docMag += kvp.Value * kvp.Value;

        if (queryMag == 0 || docMag == 0) return 0m;

        return (decimal)(dotProduct / (Math.Sqrt(queryMag) * Math.Sqrt(docMag)));
    }

    private static string Normalize(string text) =>
        text.ToLowerInvariant()
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Replace(',', ' ')
            .Replace('.', ' ');

    private static void AddOrIncrement(Dictionary<string, double> tokens, string key, double weight)
    {
        if (tokens.ContainsKey(key))
            tokens[key] += weight;
        else
            tokens[key] = weight;
    }
}
