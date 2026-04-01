# ADR-0005: Azure API Management Gateway

**Date:** 2026-04-01
**Category:** infrastructure
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

The Toromont Fleet Hub exposes a REST API consumed by the Angular frontend and potentially by third-party integrations. The API requires centralized management for rate limiting to prevent abuse and denial-of-service attacks, security header injection to comply with OWASP best practices, API documentation hosting for developer consumers, and request analytics for operational visibility. These cross-cutting concerns should be handled at the gateway level rather than within application code to ensure consistent enforcement and to decouple security policies from application deployments.

## Decision

Use Azure API Management (APIM) as the API gateway in front of the ASP.NET Core API hosted on Azure App Services:

- **Rate Limiting:** Enforce a rate limit of 100 requests per minute per IP address to prevent abuse and mitigate distributed denial-of-service attacks.
- **Security Headers:** Inject standard security headers on all API responses: `X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security`, and `Content-Security-Policy`.
- **API Documentation:** Host OpenAPI/Swagger documentation through the APIM developer portal, providing interactive API exploration for consumers.
- **Analytics and Monitoring:** Capture request metrics, latency distributions, and error rates through built-in APIM analytics integrated with Application Insights.
- **OWASP Alignment:** Rate limiting at the gateway layer complements application-level defenses including parameterized queries (SQL injection prevention), output encoding (XSS prevention), and bearer token authentication (CSRF prevention).

## Options Considered

### Option 1: Azure API Management (chosen)

- **Pros:**
  - Native Azure integration with App Services, Application Insights, and Entra ID
  - Built-in rate limiting policies configurable per IP, per subscription key, or per user without custom code
  - Managed developer portal provides interactive OpenAPI documentation out of the box
  - Policy engine allows security header injection, request/response transformation, and caching without modifying the backend API
  - Request analytics and monitoring integrated with Azure Monitor and Application Insights
  - Supports API versioning and revision management for backward-compatible evolution
  - IP filtering, JWT validation, and mutual TLS policies available as built-in capabilities

- **Cons:**
  - Adds request latency (typically 5-20ms) due to the gateway proxy hop
  - Premium tier required for virtual network integration; Developer and Standard tiers have limited network isolation
  - Consumption tier has cold start latency on first request after idle
  - Configuration complexity for advanced policy expressions using C# or Liquid templates

### Option 2: Custom ASP.NET Core Middleware

- **Pros:**
  - No additional infrastructure or Azure resource costs
  - Full control over rate limiting algorithms and header injection logic
  - Deployed as part of the existing API with no additional latency
  - Easy to unit test with standard .NET testing patterns

- **Cons:**
  - Rate limiting state must be managed manually (in-memory or distributed cache)
  - No built-in API documentation portal; requires separate Swagger UI hosting
  - Security header policies are coupled to application deployments
  - No centralized analytics dashboard; must build custom reporting
  - Does not scale across multiple App Service instances without distributed state management

### Option 3: NGINX Reverse Proxy

- **Pros:**
  - Industry-standard reverse proxy with proven performance
  - Extensive rate limiting and header manipulation capabilities
  - Open source with no licensing costs
  - Large community and extensive documentation

- **Cons:**
  - Must be deployed and managed as a separate infrastructure component (VM or container)
  - No native Azure integration; monitoring requires additional configuration
  - No built-in developer portal for API documentation
  - Configuration is file-based and requires deployment for changes
  - Operational overhead for patching, scaling, and high availability

### Option 4: Kong API Gateway

- **Pros:**
  - Feature-rich open-source API gateway with extensive plugin ecosystem
  - Supports rate limiting, authentication, logging, and transformations
  - Available as a managed service (Kong Konnect) or self-hosted

- **Cons:**
  - Additional vendor dependency outside the Azure ecosystem
  - Self-hosted deployment requires container orchestration infrastructure
  - Kong Konnect managed service adds a separate billing relationship
  - Less native integration with Azure services compared to APIM

### Option 5: AWS API Gateway

- **Pros:**
  - Mature managed API gateway with built-in rate limiting and documentation
  - Serverless pricing model with no minimum cost

- **Cons:**
  - Cross-cloud deployment contradicts the Azure-first platform decision (ADR-0001)
  - No native integration with Azure App Services, Entra ID, or Application Insights
  - Additional network latency for cross-cloud traffic
  - Separate billing and monitoring tooling required

## Consequences

### Positive

- Rate limiting at 100 requests per minute per IP provides a consistent defense layer against abuse without modifying application code
- Security headers are enforced uniformly across all API endpoints, ensuring compliance with OWASP recommendations regardless of which controller handles the request
- The developer portal provides self-service API documentation, reducing support burden for integration questions
- Centralized analytics provide visibility into API usage patterns, top consumers, error rates, and latency distributions
- Policy changes (e.g., adjusting rate limits or adding new security headers) can be deployed independently of the application

### Negative

- Additional gateway hop adds 5-20ms latency to every API request
- APIM is an additional Azure resource with monthly cost that varies by tier
- Policy configuration uses a domain-specific XML syntax that requires learning
- Two deployment pipelines: one for the API application and one for APIM policy definitions

### Risks

- Misconfigured rate limiting policies could block legitimate high-frequency users (e.g., telemetry ingestion); ensure telemetry endpoints bypass client rate limits or use separate rate limit policies
- APIM outage would make the API unreachable even if the backend App Service is healthy; configure APIM health probes and consider a failover strategy
- Developer portal exposure increases the API's attack surface by providing detailed endpoint documentation; restrict portal access to authenticated users and avoid exposing internal endpoints

## Implementation Notes

- Deploy APIM in the **Standard** tier for production, which provides sufficient throughput and SLA guarantees; evaluate Premium tier if virtual network integration is required
- Configure the following inbound policies on all API operations:
  ```xml
  <rate-limit-by-key calls="100" renewal-period="60" counter-key="@(context.Request.IpAddress)" />
  ```
- Configure the following outbound policies for security headers:
  ```xml
  <set-header name="X-Content-Type-Options" exists-action="override">
      <value>nosniff</value>
  </set-header>
  <set-header name="X-Frame-Options" exists-action="override">
      <value>DENY</value>
  </set-header>
  <set-header name="Strict-Transport-Security" exists-action="override">
      <value>max-age=31536000; includeSubDomains</value>
  </set-header>
  <set-header name="Content-Security-Policy" exists-action="override">
      <value>default-src 'self'</value>
  </set-header>
  ```
- Import the OpenAPI specification from the ASP.NET Core API's `/swagger/v1/swagger.json` endpoint
- Configure Application Insights integration for end-to-end request correlation between APIM and the backend API
- Set up Azure Monitor alerts for:
  - Rate limit violations exceeding 50 per minute (possible attack)
  - Backend response time exceeding 2 seconds (performance degradation)
  - 5xx error rate exceeding 1% over a 5-minute window
- OWASP defense-in-depth alignment:
  - **Gateway layer (APIM):** Rate limiting, security headers, IP filtering
  - **Application layer:** Parameterized queries via Dapper (SQL injection), output encoding (XSS), bearer token validation (CSRF), input validation with FluentValidation
  - **Data layer:** Encrypted connections, least-privilege database accounts

## References

- L1-011: API management and gateway requirements
- L1-013: Security and compliance requirements
- ADR-0001: Azure Cloud Platform
- [Azure API Management Documentation](https://learn.microsoft.com/en-us/azure/api-management/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [Azure API Management Policies](https://learn.microsoft.com/en-us/azure/api-management/api-management-policies)
