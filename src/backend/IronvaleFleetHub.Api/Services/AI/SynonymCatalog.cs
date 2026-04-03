namespace IronvaleFleetHub.Api.Services.AI;

public class SynonymCatalog
{
    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hydraulic cylinder"] = new[] { "ram", "actuator", "hydraulic ram", "cylinder" },
        ["hydraulic pump"] = new[] { "pump", "main pump", "hyd pump" },
        ["hydraulic hose"] = new[] { "hose", "high-pressure hose", "hyd hose" },
        ["hydraulic seal"] = new[] { "seal", "seal kit", "cylinder seal", "boom seal" },
        ["bucket tooth"] = new[] { "tooth", "edge tooth", "bucket edge", "teeth" },
        ["engine oil filter"] = new[] { "oil filter", "filter", "engine filter" },
        ["air filter"] = new[] { "air cleaner", "primary filter", "air element" },
        ["fuel filter"] = new[] { "fuel separator", "fuel water separator", "fuel element" },
        ["turbocharger"] = new[] { "turbo", "turbo assembly" },
        ["fuel injector"] = new[] { "injector", "fuel nozzle" },
        ["alternator"] = new[] { "generator", "charging system" },
        ["starter motor"] = new[] { "starter", "cranking motor" },
        ["track shoe"] = new[] { "shoe", "grouser", "track pad", "track plate" },
        ["track chain"] = new[] { "chain", "track link", "link assembly" },
        ["track roller"] = new[] { "roller", "bottom roller", "carrier roller" },
        ["drive sprocket"] = new[] { "sprocket", "final drive sprocket", "sprocket segment" },
        ["front idler"] = new[] { "idler", "idler assembly" },
        ["serpentine belt"] = new[] { "belt", "drive belt", "accessory belt" },
        ["water pump"] = new[] { "coolant pump", "engine pump" },
        ["temperature sensor"] = new[] { "temp sensor", "coolant sensor", "thermocouple" },
        ["battery"] = new[] { "bat", "12v battery", "power cell" },
        ["excavator"] = new[] { "digger", "backhoe", "trackhoe" },
        ["loader"] = new[] { "front loader", "wheel loader", "pay loader" },
        ["dozer"] = new[] { "bulldozer", "crawler", "crawler dozer" },
    };

    public IReadOnlyDictionary<string, string[]> GetAll() => Synonyms;

    public IEnumerable<string> Expand(string term)
    {
        yield return term;

        foreach (var kvp in Synonyms)
        {
            if (kvp.Key.Equals(term, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var syn in kvp.Value)
                    yield return syn;
            }
            else if (kvp.Value.Any(s => s.Equals(term, StringComparison.OrdinalIgnoreCase)))
            {
                yield return kvp.Key;
                foreach (var syn in kvp.Value.Where(s => !s.Equals(term, StringComparison.OrdinalIgnoreCase)))
                    yield return syn;
            }
        }
    }
}
