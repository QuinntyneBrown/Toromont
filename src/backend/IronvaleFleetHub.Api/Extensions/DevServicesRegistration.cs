using IronvaleFleetHub.Api.Features.Workflows;
using IronvaleFleetHub.Api.Services.AI;

namespace IronvaleFleetHub.Api.Extensions;

public static class DevServicesRegistration
{
    public static IServiceCollection AddDevAiServices(this IServiceCollection services, IConfiguration config)
    {
        var aiSection = config.GetSection("DevAi");
        var enabled = aiSection.GetValue<bool>("Enabled");

        if (!enabled) return services;

        services.Configure<DevAiOptions>(aiSection);
        services.AddScoped<AiProviderSelector>();
        services.AddScoped<IAiInsightsService, DevAiInsightsService>();
        services.AddScoped<IAiSearchService, DevNaturalLanguageSearchService>();
        services.AddScoped<RuleBasedPredictionEngine>();
        services.AddScoped<NarrativeFormatter>();
        services.AddScoped<TokenVectorizer>();
        services.AddSingleton<SynonymCatalog>();

        var enableOllama = aiSection.GetValue<bool>("EnableOllama");
        if (enableOllama)
        {
            services.AddScoped<OllamaAiInsightsService>();
            services.AddHttpClient("Ollama", client =>
            {
                client.BaseAddress = new Uri(aiSection["OllamaBaseUrl"] ?? "http://localhost:11434");
            });
        }

        return services;
    }

    public static IServiceCollection AddDevWorkflowServices(this IServiceCollection services, IConfiguration config)
    {
        var workflowSection = config.GetSection("DevWorkflows");
        var enabled = workflowSection.GetValue<bool>("Enabled");

        if (!enabled) return services;

        services.Configure<DevWorkflowOptions>(workflowSection);
        services.AddHostedService<DevWorkflowEngineHostedService>();
        services.AddScoped<IWorkflowEngine, WorkflowExecutionCoordinator>();
        services.AddScoped<WorkflowExecutionCoordinator>();
        services.AddScoped<IWorkflowDefinition, ServiceReminderWorkflow>();
        services.AddScoped<IWorkflowDefinition, WorkOrderEscalationWorkflow>();
        services.AddScoped<IWorkflowDefinition, PartsOrderStatusWorkflow>();
        services.AddScoped<ReminderCandidateQueryService>();
        services.AddScoped<EscalationPolicyService>();
        services.AddScoped<DevPartsOrderEventSource>();
        services.AddSingleton<IWorkflowClock, SystemWorkflowClock>();

        return services;
    }
}
