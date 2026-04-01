# ADR-0001: Azure OpenAI Service for AI-Powered Features

**Date:** 2026-04-01
**Category:** integration
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

The Toromont Fleet Hub platform requires AI-powered capabilities in two key areas. First, the parts ordering workflow needs natural language search so technicians can find parts using everyday language (e.g., "I need a hydraulic cylinder for my 320 excavator") rather than navigating rigid catalog hierarchies. Second, the fleet management domain benefits from predictive maintenance insights that analyze telemetry data and produce confidence-scored predictions to help prioritize service actions. These capabilities must integrate with the existing Azure-hosted infrastructure while meeting enterprise compliance requirements around data residency and governance.

## Decision

Use Azure OpenAI Service as the AI backbone for natural language parts search (via embeddings) and predictive maintenance insights across the fleet management platform.

### AI Feature Specifications

- **Parts Catalog Natural Language Search:** Azure OpenAI embeddings power semantic search against the parts catalog. When the embedding service is unavailable or returns low-confidence results, the system falls back to SQL full-text search with fuzzy matching to ensure uninterrupted parts discovery.
- **Predictive Maintenance:** A batch process runs daily at 2 AM UTC, analyzing telemetry data to generate maintenance predictions. Confidence scoring drives prioritization: predictions with confidence >= 80% are classified as High Priority, while those < 50% are flagged as Low Confidence and require manual review.
- **Anomaly Detection:** Temperature anomalies are identified when readings exceed 2 standard deviations from the 30-day rolling average. Fuel consumption anomalies trigger when usage increases by more than 30% compared to the 7-day rolling average.
- **Cold Start Requirement:** The predictive maintenance and anomaly detection features require a minimum of 30 days of historical telemetry data before producing reliable results.

## Options Considered

### Option 1: Azure OpenAI Service (chosen)

- **Pros:**
  - Native integration with the existing Azure ecosystem (Azure SQL, Azure Functions, Azure AD)
  - Enterprise compliance: all data remains within the Azure tenant, satisfying data residency requirements
  - Embedding-based semantic search with SQL full-text search fallback ensures high availability
  - Single platform supports both search (embeddings) and prediction (completions) use cases
  - Microsoft enterprise support and SLA guarantees
  - Managed service reduces operational burden

- **Cons:**
  - Higher cost compared to some alternatives at scale
  - Model availability tied to Microsoft's Azure OpenAI rollout schedule
  - Vendor lock-in to Azure ecosystem for AI capabilities

### Option 2: OpenAI Direct API

- **Pros:**
  - Access to latest models immediately upon release
  - Well-documented API with broad community support
  - Flexible pricing tiers

- **Cons:**
  - Data leaves the Azure tenant, creating compliance concerns
  - No native integration with Azure services (separate authentication, networking)
  - Less enterprise-grade SLA guarantees compared to Azure OpenAI

### Option 3: AWS Bedrock

- **Pros:**
  - Access to multiple foundation models (Claude, Titan, Llama)
  - Managed service with enterprise features

- **Cons:**
  - Requires cross-cloud data movement from the Azure-hosted platform
  - Additional complexity managing a second cloud provider
  - No integration with existing Azure AD and networking

### Option 4: Google Vertex AI

- **Pros:**
  - Strong ML tooling and model garden
  - Competitive pricing

- **Cons:**
  - Same cross-cloud concerns as AWS Bedrock
  - Smallest enterprise market share among the three major clouds
  - No integration with existing Azure infrastructure

### Option 5: Open-Source Models (Llama)

- **Pros:**
  - No per-token API costs
  - Full control over model and data
  - No vendor lock-in

- **Cons:**
  - Significant infrastructure overhead for hosting and scaling GPU compute
  - Requires dedicated ML engineering team for fine-tuning and maintenance
  - Lower out-of-the-box quality for embedding-based search compared to OpenAI models

### Option 6: Rule-Based Only (No AI)

- **Pros:**
  - Simplest implementation, no AI infrastructure required
  - Deterministic and fully explainable results
  - No ongoing model costs

- **Cons:**
  - Cannot support natural language parts search
  - Predictive maintenance limited to static threshold rules
  - Poor user experience for parts discovery
  - Does not satisfy L1-006 (AI-Powered Insights) requirements

## Consequences

### Positive

- Technicians can search parts using natural language, significantly reducing time-to-find for parts ordering
- Predictive maintenance with confidence scoring enables proactive fleet servicing and prioritized work orders
- Anomaly detection provides early warning for equipment issues before they become failures
- Seamless integration with existing Azure infrastructure minimizes architectural complexity
- Enterprise data governance requirements are met without additional tooling

### Negative

- Dependency on Azure OpenAI service availability and rate limits
- Embedding model updates may require re-indexing the parts catalog
- Predictive maintenance accuracy is limited during the 30-day cold start period for new equipment

### Risks

- Azure OpenAI rate limits could throttle parts search during peak usage; mitigated by the SQL full-text search fallback
- Model drift over time may degrade prediction quality; mitigated by scheduled model evaluation and retraining cycles
- Cold start period leaves new equipment without predictive coverage for the first 30 days

## Implementation Notes

- **Resolved:** Use rule-based heuristics for the initial release. No ML model training in v1. Azure OpenAI is used for natural language parts search (embeddings) only. Anomaly detection and predictive maintenance use deterministic rules (temperature >2σ deviation, fuel >30% increase, operating pattern analysis). ML models may be introduced in a future release once sufficient training data is accumulated, but the v1 decision is rule-based only.
- Parts search should implement a graceful degradation chain: Azure OpenAI embeddings -> SQL full-text search with fuzzy matching -> exact keyword search.
- Predictive maintenance batch job at 2 AM UTC should be implemented as an Azure Function with a timer trigger.
- Anomaly detection thresholds (2 sigma for temperature, 30% for fuel) are configured globally, not per equipment model. One set of rules applies across all equipment to simplify initial implementation.

## References

- L1-006: AI-Powered Insights
- L1-004: Parts Ordering and Inventory
- Azure OpenAI Service documentation: https://learn.microsoft.com/en-us/azure/ai-services/openai/
