using Microsoft.Extensions.Options;

namespace IronvaleFleetHub.Api.Services.AI;

public class AiProviderSelector
{
    private readonly DevAiOptions _options;
    private readonly IServiceProvider _services;
    private readonly ILogger<AiProviderSelector> _logger;

    public AiProviderSelector(
        IOptions<DevAiOptions> options,
        IServiceProvider services,
        ILogger<AiProviderSelector> logger)
    {
        _options = options.Value;
        _services = services;
        _logger = logger;
    }

    public IAiInsightsService ResolveInsightsService()
    {
        if (_options.EnableOllama && _options.InsightsMode == AiProviderMode.Ollama)
        {
            var ollama = _services.GetService<OllamaAiInsightsService>();
            if (ollama != null)
            {
                _logger.LogDebug("Using Ollama AI insights provider");
                return ollama;
            }
            _logger.LogWarning("Ollama requested but not available, falling back to rule-based");
        }

        _logger.LogDebug("Using rule-based AI insights provider");
        return _services.GetRequiredService<DevAiInsightsService>();
    }

    public IAiSearchService ResolveSearchService()
    {
        _logger.LogDebug("Using dev natural language search provider");
        return _services.GetRequiredService<DevNaturalLanguageSearchService>();
    }
}
