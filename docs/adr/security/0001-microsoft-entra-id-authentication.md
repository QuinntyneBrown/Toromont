# ADR-0001: Microsoft Entra ID Authentication with OAuth2 Authorization Code Flow + PKCE

**Date:** 2026-04-01
**Category:** security
**Status:** Accepted
**Deciders:** Ironvale Fleet Hub Architecture Team

## Context

Ironvale Fleet Hub is an enterprise fleet management system that requires secure, standards-based authentication capable of supporting Single Sign-On (SSO) across the organization. Ironvale operates within the Microsoft ecosystem, and the system must accommodate four distinct user roles: Admin, Fleet Manager, Operator, and Technician. The authentication mechanism must protect against common web vulnerabilities, support silent token renewal for a seamless user experience, and enforce session timeout policies appropriate for an industrial fleet management context.

## Decision

Use Microsoft Entra ID (formerly Azure AD) as the identity provider, implementing the OAuth2 Authorization Code flow with Proof Key for Code Exchange (PKCE) for authentication. On the frontend, integrate `@azure/msal-angular` with `MsalGuard` for route protection and `MsalInterceptor` for automatic bearer token injection on outbound API requests. On the backend, validate JWT bearer tokens using RS256 signature verification against Microsoft Entra ID's published signing keys. Store tokens in memory only (never in localStorage or sessionStorage) to mitigate XSS-based token theft. Enforce a 30-minute sliding session timeout with silent token renewal via a hidden iframe.

## Options Considered

### Option 1: Microsoft Entra ID with OAuth2 Authorization Code + PKCE (chosen)

- **Pros:**
  - Native integration with Azure services and the broader Microsoft ecosystem already in use at Ironvale
  - Enterprise-grade SSO support with federated identity capabilities
  - PKCE extension prevents authorization code interception attacks, eliminating the need for a client secret in the SPA
  - Silent token renewal via hidden iframe provides seamless session continuity
  - Built-in support for Conditional Access policies, MFA, and compliance controls
  - Well-supported by `@azure/msal-angular` library with Angular route guards and HTTP interceptors

- **Cons:**
  - Tightly couples the authentication layer to Microsoft Entra ID
  - Configuration complexity with tenant registration, redirect URIs, and scope definitions
  - Requires ongoing management of app registrations in the Azure portal

### Option 2: Auth0

- **Pros:**
  - Vendor-agnostic, supports multiple identity providers out of the box
  - Developer-friendly SDKs and extensive documentation
  - Built-in user management dashboard

- **Cons:**
  - Additional licensing cost on top of existing Microsoft 365/Azure subscriptions
  - Introduces a third-party dependency outside the Microsoft ecosystem
  - Does not leverage existing Entra ID directory and group memberships directly

### Option 3: AWS Cognito

- **Pros:**
  - Cost-effective for high-volume user pools
  - Integrates well with AWS-hosted backends

- **Cons:**
  - Misaligned with Ironvale's Microsoft-centric infrastructure
  - Weaker enterprise SSO and federation capabilities compared to Entra ID
  - Would require bridging between AWS and Azure identity layers

### Option 4: Firebase Auth

- **Pros:**
  - Simple setup for consumer-facing applications
  - Free tier available for small user bases

- **Cons:**
  - Designed primarily for consumer apps, not enterprise fleet management
  - Limited enterprise SSO and Conditional Access support
  - No native integration with Microsoft ecosystem

### Option 5: Custom JWT Authentication

- **Pros:**
  - Full control over token format, claims, and validation logic
  - No external identity provider dependency

- **Cons:**
  - Significant development and maintenance burden for a security-critical component
  - Must independently implement token signing, rotation, revocation, and session management
  - Lacks built-in MFA, Conditional Access, and SSO capabilities
  - Higher risk of security vulnerabilities in a custom implementation

## Consequences

### Positive

- Users authenticate with their existing Ironvale Microsoft credentials, reducing credential fatigue and onboarding friction
- PKCE-protected authorization code flow is the current best practice for SPAs, aligning with OAuth 2.1 recommendations
- Memory-only token storage eliminates the XSS attack vector against persisted tokens
- Silent token renewal provides uninterrupted sessions without forcing re-authentication during active use
- Conditional Access and MFA policies can be enforced centrally through Entra ID without application-level changes

### Negative

- Vendor lock-in to Microsoft Entra ID as the identity provider; migrating away would require significant rework of authentication flows on both frontend and backend
- Developers must understand MSAL library internals, Entra ID app registration, and token lifecycle management
- Silent renewal via hidden iframe can fail in browsers with aggressive third-party cookie blocking, requiring fallback handling

### Risks

- If Microsoft Entra ID experiences an outage, users cannot authenticate until service is restored; no offline authentication fallback is provided
- Token lifetime and session policies must be carefully configured to balance security (shorter lifetimes) with usability (fewer re-authentication prompts)
- Changes to Microsoft's MSAL libraries or Entra ID token format could require frontend updates on Microsoft's release cadence

## Implementation Notes

- Register the Ironvale Fleet Hub as a Single Page Application in Microsoft Entra ID with the appropriate redirect URIs for each environment (development, staging, production)
- Configure `MsalGuard` on all authenticated Angular routes to enforce login before navigation
- Configure `MsalInterceptor` to attach bearer tokens to all API calls matching the backend's base URL
- Backend ASP.NET Core API validates JWT bearer tokens using `Microsoft.Identity.Web`, verifying issuer, audience, and RS256 signature against Entra ID's JWKS endpoint
- Set access token lifetime to 30 minutes with sliding expiration; use refresh tokens for silent renewal
- Implement a session inactivity timeout of 30 minutes that triggers re-authentication
- Log all authentication events (login, logout, token renewal, failure) for audit trail purposes

## References

- L1-010: Authentication & Authorization
- L1-013: Security & Compliance
- [Microsoft Identity Platform and OAuth 2.0 Authorization Code Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow)
- [MSAL Angular Configuration](https://github.com/AzureAD/microsoft-authentication-library-for-js/tree/dev/lib/msal-angular)
- [RFC 7636 - Proof Key for Code Exchange (PKCE)](https://datatracker.ietf.org/doc/html/rfc7636)
