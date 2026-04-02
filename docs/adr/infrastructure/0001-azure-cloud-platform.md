# ADR-0001: Azure Cloud Platform

**Date:** 2026-04-01
**Category:** infrastructure
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

The Ironvale Fleet Hub is an enterprise fleet management system that requires reliable cloud hosting with integrated services spanning compute, serverless processing, workflow automation, monitoring, and AI capabilities. The platform must support a .NET backend, Microsoft Entra ID authentication, and a broad suite of managed services including application hosting with auto-scaling, serverless functions, email and SMS communication, AI inference, and centralized API management. Ironvale operates within the Microsoft ecosystem, making alignment with Microsoft tooling and identity infrastructure a significant consideration.

## Decision

Use Microsoft Azure as the cloud platform for the Ironvale Fleet Hub. The deployment leverages the following Azure services:

- **Azure App Services** for hosting the main ASP.NET Core API with managed auto-scaling
- **Azure Functions** for serverless telemetry ingestion and scheduled AI workloads
- **Azure Logic Apps** for workflow automation (service reminders, escalation)
- **Azure Application Insights** for application performance monitoring and diagnostics
- **Azure API Management** for centralized gateway, rate limiting, and API documentation
- **Azure Communication Services** for email and SMS notification delivery
- **Azure OpenAI Service** for predictive maintenance and anomaly detection models

## Options Considered

### Option 1: Microsoft Azure (chosen)

- **Pros:**
  - Native integration with Microsoft Entra ID, which Ironvale already uses for identity and access management
  - First-class support for .NET and ASP.NET Core workloads on App Services
  - Integrated service suite (Functions, Logic Apps, Communication Services, OpenAI) reduces cross-vendor integration complexity
  - App Services provides managed hosting with built-in auto-scaling, deployment slots, and health checks
  - Single billing relationship and unified management portal for all services
  - Enterprise support agreements and SLAs aligned with Ironvale's existing Microsoft licensing

- **Cons:**
  - Vendor lock-in to the Azure ecosystem
  - Some Azure services carry premium pricing compared to alternatives
  - Regional availability of certain services (e.g., Azure OpenAI) may impose geographic constraints

### Option 2: Amazon Web Services (AWS)

- **Pros:**
  - Broadest service catalog and largest cloud market share
  - Mature serverless ecosystem (Lambda, Step Functions)
  - Competitive pricing with reserved instance discounts

- **Cons:**
  - No native Microsoft Entra ID integration; would require federated identity configuration
  - .NET support is secondary to other runtimes in the AWS ecosystem
  - Additional integration effort to bridge AWS services with Microsoft tooling
  - Ironvale would need to manage a separate vendor relationship

### Option 3: Google Cloud Platform

- **Pros:**
  - Strong data analytics and machine learning capabilities
  - Competitive pricing on compute and storage
  - Kubernetes-native approach with GKE

- **Cons:**
  - Weakest enterprise Microsoft ecosystem integration among the three major clouds
  - Smaller market share means fewer third-party integrations and community resources
  - .NET support is less mature compared to Azure
  - No equivalent to Azure Communication Services for unified email/SMS

### Option 4: On-Premises Hosting

- **Pros:**
  - Full control over infrastructure and data residency
  - No recurring cloud consumption costs
  - No external network dependency for core operations

- **Cons:**
  - Significant capital expenditure for hardware procurement and data center space
  - Operational burden for patching, scaling, disaster recovery, and high availability
  - No managed equivalents for serverless functions, logic apps, or AI services
  - Scaling to handle telemetry burst traffic would require over-provisioning hardware

## Consequences

### Positive

- Seamless integration with Microsoft Entra ID eliminates the need for identity bridging or federation
- .NET workloads run on a platform optimized for the runtime, reducing configuration overhead
- Managed services (App Services, Functions, Logic Apps) shift operational burden to Azure, freeing the team to focus on application logic
- Unified monitoring through Application Insights provides end-to-end request tracing across all services
- Azure OpenAI Service provides access to GPT models without managing ML infrastructure

### Negative

- Vendor lock-in means migrating away from Azure would require significant re-architecture of serverless functions, logic apps, and communication integrations
- Azure pricing for premium services (API Management, OpenAI) must be monitored to avoid cost overruns
- Team members unfamiliar with Azure will require onboarding and training

### Risks

- Azure service outages would affect the entire platform; mitigate with multi-region deployment readiness and health monitoring
- Azure OpenAI regional availability could limit deployment options; monitor Azure roadmap for expanded regions
- Cost growth as telemetry volume scales; establish budget alerts and review consumption-based pricing monthly

## Implementation Notes

- Provision all Azure resources using Infrastructure as Code (ARM templates or Bicep) for repeatable deployments
- Configure Azure App Services with staging deployment slots for zero-downtime releases
- Enable Application Insights auto-instrumentation for the ASP.NET Core API and Azure Functions
- Set up Azure Monitor alerts for service health, request failure rates, and cost thresholds
- Use Azure Key Vault for secrets management across all services

## References

- L1-011: Cloud hosting and infrastructure requirements
- L1-012: Platform scalability and reliability requirements
- [Azure App Services Documentation](https://learn.microsoft.com/en-us/azure/app-service/)
- [Azure Well-Architected Framework](https://learn.microsoft.com/en-us/azure/well-architected/)
