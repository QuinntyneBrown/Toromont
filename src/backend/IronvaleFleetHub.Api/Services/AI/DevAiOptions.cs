namespace IronvaleFleetHub.Api.Services.AI;

public sealed class DevAiOptions
{
    public bool Enabled { get; set; } = true;
    public AiProviderMode InsightsMode { get; set; } = AiProviderMode.RuleBased;
    public AiProviderMode SearchMode { get; set; } = AiProviderMode.Mock;
    public bool EnableOllama { get; set; }
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public int CacheSeconds { get; set; } = 30;
}

public enum AiProviderMode
{
    Mock,
    RuleBased,
    Ollama
}
