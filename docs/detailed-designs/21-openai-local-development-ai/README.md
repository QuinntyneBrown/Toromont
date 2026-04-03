# Azure OpenAI Local Development AI — Detailed Design

## 1. Overview

**Source:** `docs/local-development-strategy.md` Section 4.3 identifies Azure OpenAI Service as a dependency without an official local emulator and recommends a mock service by default.

Azure OpenAI appears in the Fleet Hub architecture in two distinct ways:

- natural-language parts search
- AI-driven maintenance insights and anomaly narratives

Local development needs those features to behave meaningfully without making outbound cloud calls, while keeping the same API contracts that the frontend and batch jobs already expect.

**Scope:** introduce a development-only AI provider stack that keeps the production interfaces intact but swaps Azure OpenAI for deterministic local implementations by default, with an optional Ollama adapter for developers who want a higher-fidelity local model.

**Goals:**

- zero required cloud dependency for AI features in local development
- stable contracts for parts search and maintenance insight endpoints
- deterministic responses suitable for integration tests
- optional higher-fidelity local model integration without changing application code

**References:**

- [Local Development Strategy](../../local-development-strategy.md)
- [ADR-0001 Azure OpenAI AI-Powered Features](../../adr/integration/0001-azure-openai-ai-powered-features.md)
- [Feature 04 Parts & Ordering](../04-parts-ordering/README.md)
- [Feature 06 AI Insights](../06-ai-insights/README.md)

## 2. Architecture

### 2.1 Runtime Components

The local replacement is split into four major pieces:

- `AiProviderSelector` chooses mock, rule-based, or optional Ollama-backed providers
- `LocalAiInsightsService` produces deterministic maintenance predictions and anomaly narratives
- `LocalNaturalLanguageSearchService` approximates semantic search using tokenization, synonym expansion, and weighted ranking
- `AiScenarioRepository` supplies repeatable seeded scenarios for tests and UI development

![Component Diagram](diagrams/c4_component.png)

### 2.2 Canonical Prediction Flow

1. The API or prediction batch job requests AI insights for an equipment item.
2. `AiProviderSelector` resolves the configured local mode.
3. `LocalAiInsightsService` loads telemetry aggregates and service history.
4. `RuleBasedPredictionEngine` computes confidence and issue candidates.
5. `NarrativeFormatter` creates the explanation text returned to the API.
6. Results are cached briefly for local responsiveness and deterministic test behavior.

![Sequence Diagram](diagrams/sequence_prediction_flow.png)

### 2.3 Class Diagram

![Class Diagram](diagrams/class_diagram.png)

## 3. Components, Types, and Classes

### 3.1 Provider Selection

#### `AiProviderMode`

- **Type:** enum
- **Values:**
  - `Mock`
  - `RuleBased`
  - `Ollama`
- **Purpose:** controls which local implementation is active

#### `AiProviderSelector`

- **Type:** composition service
- **Responsibility:** resolves the active AI providers from configuration
- **Rules:**
  - default to `RuleBased` for insights and `Mock` for narrative embellishment
  - use `Ollama` only when explicitly enabled
  - never call Azure OpenAI when `IHostEnvironment.IsDevelopment()` and local mode is enabled

### 3.2 Insights Services

#### `IAiInsightsService`

- **Type:** application boundary
- **Responsibility:** returns predictive maintenance and anomaly insight contracts used by controllers and batch jobs
- **Key members:**

```csharp
public interface IAiInsightsService
{
    Task<MaintenancePrediction> PredictMaintenanceAsync(Guid equipmentId, CancellationToken ct = default);
    Task<IReadOnlyList<AnomalyInsight>> DetectAnomaliesAsync(Guid equipmentId, CancellationToken ct = default);
}
```

#### `LocalAiInsightsService`

- **Type:** default development implementation
- **Responsibility:** coordinates telemetry loading, rule evaluation, narrative formatting, and optional scenario overrides
- **Behavior:**
  - checks `AiScenarioRepository` for an explicit seeded response
  - otherwise computes a deterministic result from telemetry aggregates
  - formats the human-readable explanation without an LLM by default

#### `RuleBasedPredictionEngine`

- **Type:** domain service
- **Responsibility:** converts telemetry and service-history aggregates into a prediction score and recommended action
- **Inputs:**
  - temperature deviation
  - fuel variance
  - repeated fault counts
  - days since last service
- **Output:** `PredictionEvidence`

#### `NarrativeFormatter`

- **Type:** formatter service
- **Responsibility:** turns evidence into explanation text such as:
  - predicted issue
  - confidence summary
  - recommended action
  - priority reason

This keeps the API payload shape useful for the frontend even without an LLM.

#### `OllamaAiInsightsService`

- **Type:** optional development implementation
- **Responsibility:** calls a local Ollama endpoint for richer explanation text or search expansion
- **Constraint:** optional only; the default local experience must not require Ollama

### 3.3 Parts Search Services

#### `IAiSearchService`

- **Type:** application boundary
- **Responsibility:** supports natural-language parts search
- **Key member:**

```csharp
public interface IAiSearchService
{
    Task<IReadOnlyList<AiSearchResult>> SearchPartsAsync(string query, CancellationToken ct = default);
}
```

#### `LocalNaturalLanguageSearchService`

- **Type:** default development implementation
- **Responsibility:** approximates semantic parts search without embeddings
- **Pipeline:**
  - normalize query text
  - tokenize and stem words
  - expand terms from `SynonymCatalog`
  - issue SQL or in-memory filtering query
  - re-rank matches using weighted lexical similarity and equipment compatibility

#### `TokenVectorizer`

- **Type:** helper class
- **Responsibility:** converts query and catalog fields into weighted token maps
- **Why used:** gives a deterministic, embedding-free approximation of relevance scoring

#### `SynonymCatalog`

- **Type:** static data service
- **Responsibility:** maps domain terms such as:
  - `hydraulic cylinder -> ram, actuator`
  - `bucket tooth -> tooth, edge tooth`
  - `engine oil filter -> oil filter, filter`

This preserves some of the value of natural language search locally.

### 3.4 Seeded Scenario Support

#### `AiScenarioRepository`

- **Type:** infrastructure abstraction
- **Responsibility:** returns deterministic scenario overrides for development demos and tests
- **Possible stores:**
  - JSON file under `dev-data`
  - SQL table such as `DevAiScenarios`

#### `AiScenarioRecord`

- **Type:** DTO / persistence model
- **Fields:**
  - `Guid EquipmentId`
  - `string ScenarioType`
  - `string PredictedIssue`
  - `decimal ConfidenceScore`
  - `string RecommendedAction`

### 3.5 Result Contracts and Option Types

#### `MaintenancePrediction`

- **Type:** API contract
- **Fields:**
  - `Guid EquipmentId`
  - `decimal ConfidenceScore`
  - `string PredictedIssue`
  - `string RecommendedAction`
  - `string Priority`
  - `string Explanation`

#### `AnomalyInsight`

- **Type:** API contract
- **Fields:**
  - `string Metric`
  - `decimal Baseline`
  - `decimal CurrentValue`
  - `decimal DeviationPercent`
  - `string Severity`
  - `string Explanation`

#### `AiSearchResult`

- **Type:** API contract
- **Fields:**
  - `Guid PartId`
  - `string PartNumber`
  - `string Description`
  - `decimal Score`
  - `string MatchReason`

#### `LocalAiOptions`

- **Type:** options class

```csharp
public sealed class LocalAiOptions
{
    public bool Enabled { get; set; } = true;
    public AiProviderMode InsightsMode { get; set; } = AiProviderMode.RuleBased;
    public AiProviderMode SearchMode { get; set; } = AiProviderMode.Mock;
    public bool EnableOllama { get; set; }
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public int CacheSeconds { get; set; } = 30;
}
```

## 4. Detailed Behavior

### 4.1 Predictive Maintenance

- Load the last 30 days of telemetry aggregates when available.
- If fewer than 30 days exist, return a low-confidence result with `Priority = Low` and an explanation that the equipment is in cold-start mode.
- Apply deterministic scoring based on thresholds already documented in the AI insights design.
- Format the response so the frontend still receives a human-readable recommendation.

### 4.2 Anomaly Detection

- Temperature anomalies trigger when readings exceed the rolling mean by more than 2 standard deviations.
- Fuel anomalies trigger when current usage exceeds the 7-day baseline by more than 30%.
- Explanation text comes from `NarrativeFormatter`, not from a remote model.

### 4.3 Parts Search

- Query text is normalized and tokenized.
- `SynonymCatalog` expands domain-specific equivalents.
- A SQL or in-memory candidate set is loaded from part descriptions, numbers, and compatibility tags.
- `TokenVectorizer` calculates a weighted similarity score.
- Results include a `MatchReason` field so developers can understand why a part ranked highly.

### 4.4 Optional Ollama Mode

- If enabled, `OllamaAiInsightsService` or an Ollama-backed search expander may enhance explanation text.
- If the Ollama endpoint is unavailable, the selector falls back to the default local provider and logs a warning.
- No feature should fail hard because Ollama is missing.

## 5. Acceptance Tests

### 5.1 Deterministic Prediction

- Given seeded telemetry with a high temperature deviation, when `PredictMaintenanceAsync` is called, then the returned confidence score, issue text, and priority match the expected deterministic output.

### 5.2 Cold Start Behavior

- Given fewer than 30 days of telemetry, when `PredictMaintenanceAsync` is called, then the result is low confidence and explicitly indicates cold-start mode.

### 5.3 Natural Language Parts Search

- Given the query `hydraulic ram for 320 excavator`, when `SearchPartsAsync` is called, then results include hydraulic cylinder parts with a non-zero score and a `MatchReason` derived from synonym expansion.

### 5.4 Ollama Fallback

- Given `EnableOllama = true` but the local endpoint is unreachable, when insights are requested, then the service returns the rule-based result and records a warning instead of failing the request.

## 6. Security Considerations

- Local mode must disable outbound Azure OpenAI calls by default.
- Optional Ollama mode should stay on `localhost` unless explicitly configured otherwise.
- AI explanations must not leak cross-tenant data when aggregating telemetry.
- Scenario fixtures and cached results should stay in development-only stores.

## 7. Open Questions

1. Should the synonym catalog live in code, JSON, or a database table for easier updates?
2. Is the rule-based local search good enough, or do we want to support local embeddings in a later iteration?
3. Should the optional Ollama integration cover only explanation text, or also parts-search query expansion?
