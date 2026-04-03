# Communication Services Local Delivery — Detailed Design

## 1. Overview

**Source:** `docs/local-development-strategy.md` Section 4.2 identifies Azure Communication Services as a service without an official local emulator and recommends local email and SMS stubs.

The production architecture uses Azure Communication Services for outbound email and SMS delivery. Local development needs the same notification flows to execute without sending real messages, while still honoring preferences, rendering templates, capturing outputs, and recording delivery attempts for troubleshooting.

**Scope:** extend the existing `NotificationDispatchService` with concrete email and SMS channel implementations for local development, preserving the production notification contract while routing email to a local SMTP capture target or file drop, routing SMS to a console and entity-backed sink, and recording every attempt for inspection.

**Goals:**

- Keep notification-triggering code unchanged across environments
- Extend the existing `NotificationDispatchService` email/SMS hooks with concrete channel implementations
- Support email capture without real delivery
- Support SMS inspection without carrier integration
- Preserve user preferences, templates, and audit logs locally

**References:**

- [Local Development Strategy](../../local-development-strategy.md)
- [ADR-0002 Azure Communication Services Notifications](../../adr/integration/0002-azure-communication-services-notifications.md)
- [Feature 07 Notifications & Reporting](../07-notifications-reporting/README.md)
- [Notification Contract Unification](../15-notification-contract-unification/README.md)
- [Design 19 Logic Apps Local Workflow Engine](../19-logic-apps-local-workflow-engine/README.md)

> **Naming convention:** all development-only types use the `Dev*` prefix (e.g., `DevSmtpEmailChannel`, `DevNotificationController`) to match the existing codebase convention established by `DevAuthHandler`.

## 2. Architecture

### 2.1 Runtime Components

The development delivery stack extends the existing `NotificationDispatchService` (`src/backend/IronvaleFleetHub.Api/Services/NotificationDispatchService.cs`) rather than replacing it. The existing service already:

- Creates `Notification` records in the database (lines 45–61)
- Broadcasts via SignalR to `user-{userId}` groups (lines 64–83)
- Checks `NotificationPreference.EmailEnabled` and `SmsEnabled` per user (lines 88–102) but **logs instead of dispatching**

This design wires concrete channel implementations into those email/SMS hooks:

- `IEmailChannel` and `ISmsChannel` interfaces injected into `NotificationDispatchService`
- `DevSmtpEmailChannel` to send email to MailHog or smtp4dev
- `FileDropEmailChannel` as a no-dependency fallback when SMTP is unavailable
- `CompositeEmailChannel` to try SMTP first and fall back to file drop
- `ConsoleSmsChannel` to write SMS messages to logs and a local entity store
- `NotificationTemplateRenderer` to create channel-specific subjects and bodies
- `DeliveryAttemptRecord` entity in `FleetHubDbContext` to capture each attempt and result

![Component Diagram](diagrams/c4_component.png)

### 2.2 Canonical Delivery Flow

1. A workflow or domain event calls `INotificationDispatchService.DispatchAsync(userId, type, title, message)` — the existing entry point used everywhere.
2. The existing service creates the in-app `Notification` record and broadcasts via SignalR (unchanged).
3. The existing service checks `NotificationPreference` for email/SMS eligibility (unchanged).
4. **New:** if `EmailEnabled`, the service calls `IEmailChannel.SendAsync()` which routes to local SMTP or file drop.
5. **New:** if `SmsEnabled`, the service calls `ISmsChannel.SendAsync()` which writes to console and persists for inspection.
6. **New:** delivery outcomes are stored as `DeliveryAttemptRecord` entities in `FleetHubDbContext`.

![Sequence Diagram](diagrams/sequence_delivery_flow.png)

### 2.3 Class Diagram

![Class Diagram](diagrams/class_diagram.png)

## 3. Components, Types, and Classes

### 3.1 Core Abstractions

#### `NotificationDispatchService` (Existing — Extended)

- **File:** `src/backend/IronvaleFleetHub.Api/Services/NotificationDispatchService.cs`
- **Type:** existing orchestration service — this design extends it, not replaces it
- **Current behavior (lines 88–102):** checks `NotificationPreference.EmailEnabled` and `SmsEnabled`, then **logs** a placeholder message
- **Extended behavior:** the existing log-only email/SMS hooks are replaced with calls to injected `IEmailChannel` and `ISmsChannel` implementations. The service gains two new constructor dependencies:

```csharp
public NotificationDispatchService(
    FleetHubDbContext db,
    IHubContext<NotificationHub> hubContext,
    ILogger<NotificationDispatchService> logger,
    IEmailChannel? emailChannel = null,   // new — nullable for backward compatibility
    ISmsChannel? smsChannel = null)        // new — nullable for backward compatibility
```

When `emailChannel` is null (production without ACS wiring), the existing log-only behavior is preserved. When injected (local development), actual delivery occurs.

#### `IEmailChannel`

- **Type:** delivery-channel interface
- **Responsibility:** send one rendered email message to a development sink
- **Implementations:** `DevSmtpEmailChannel`, `FileDropEmailChannel`

#### `ISmsChannel`

- **Type:** delivery-channel interface
- **Responsibility:** write one rendered SMS message to a development sink
- **Implementation:** `ConsoleSmsChannel`

#### `INotificationTemplateRenderer`

- **Type:** application service
- **Responsibility:** render channel-specific message content from shared templates and tokens
- **Recommended implementation:** `NotificationTemplateRenderer` backed by RazorLight or a simple token replacement engine

### 3.2 Preference and Template Services

#### `NotificationPreferenceEvaluator`

- **Type:** policy service
- **Responsibility:** centralizes rules such as:
  - email disabled for service reminders
  - SMS enabled only for critical equipment alerts
  - in-app only for low-priority updates

By centralizing the rules, local and production delivery choose the same channels.

### 3.3 Email Channel Implementations

#### `DevSmtpEmailChannel`

- **Type:** infrastructure adapter
- **Responsibility:** sends email to MailHog, smtp4dev, or any developer-configured SMTP server
- **Primary configuration:**
  - `Host`
  - `Port`
  - `UseSsl`
  - `FromAddress`
- **Expected usage:** MailHog on `localhost:1025`

#### `FileDropEmailChannel`

- **Type:** infrastructure adapter
- **Responsibility:** writes `.eml` or `.html` artifacts into a configured local folder
- **Why needed:** preserves zero-Docker local development when no SMTP capture tool is running
- **Output example:** `artifacts/dev-emails/20260403T181500Z-work-order-assigned.eml`

#### `CompositeEmailChannel`

- **Type:** channel combiner
- **Responsibility:** tries SMTP first and falls back to file drop if SMTP is unavailable
- **Benefit:** developers still see email output even when MailHog is not started

### 3.4 SMS Channel Implementation

#### `ConsoleSmsChannel`

- **Type:** infrastructure adapter
- **Responsibility:** writes the phone number and message body to structured logs and stores the message in a local repository
- **Why repository-backed:** console output is transient; developers need a queryable history for tests and manual verification

#### `DevSmsMessageRecord` (DbContext Entity)

- **Type:** EF Core entity registered in `FleetHubDbContext`
- **Responsibility:** stores SMS capture rows with timestamp, recipient, message, and originating event type
- **Persistence:** add `DbSet<DevSmsMessageRecord>` to `FleetHubDbContext`, consistent with existing entity access patterns

> **Architectural note:** like Design #19's workflow entities, this follows the existing pattern of direct `DbSet<T>` access via `FleetHubDbContext` rather than introducing a separate repository abstraction.

### 3.5 Delivery Logging and Inspection

#### `DeliveryAttemptRecord` (DbContext Entity)

- **Type:** EF Core entity registered in `FleetHubDbContext`
- **Responsibility:** stores each attempt per channel, including:
  - recipient
  - event type
  - channel
  - success
  - failure reason
  - provider target (`smtp`, `file-drop`, `console`)
- **Persistence:** add `DbSet<DeliveryAttemptRecord>` to `FleetHubDbContext`

#### `DevNotificationController`

- **Type:** development-only API controller
- **Responsibility:** exposes inspection endpoints such as:
  - `GET /api/dev/notifications/emails`
  - `GET /api/dev/notifications/sms`
  - `GET /api/dev/notifications/deliveries`

This controller is optional but strongly recommended because SMS otherwise has no visual inbox equivalent to MailHog.

### 3.6 Data Contracts and Option Types

#### `NotificationEnvelope`

- **Type:** top-level delivery DTO
- **Fields:**
  - `Guid UserId`
  - `string EventType`
  - `string Title`
  - `string Message`
  - `Dictionary<string, string> Tokens`
  - `NotificationPriority Priority`

#### `EmailDeliveryRequest`

- **Type:** rendered-email DTO
- **Fields:**
  - `string To`
  - `string Subject`
  - `string HtmlBody`
  - `string TextBody`

#### `SmsDeliveryRequest`

- **Type:** rendered-SMS DTO
- **Fields:**
  - `string To`
  - `string Message`

#### `DeliveryAttemptResult`

- **Type:** result DTO
- **Fields:**
  - `bool Success`
  - `string Channel`
  - `string Target`
  - `string? Error`

#### `DevNotificationDeliveryOptions`

- **Type:** configuration class bound via `IConfiguration`

```csharp
public sealed class DevNotificationDeliveryOptions
{
    public bool Enabled { get; set; }
    public bool UseSmtp { get; set; } = true;
    public string? SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public string EmailDropFolder { get; set; } = "artifacts/dev-emails";
    public bool EnableSmsConsoleSink { get; set; } = true;
}
```

> **Configuration pattern:** following the existing codebase convention, configuration values are read from `builder.Configuration` in `Program.cs` rather than injected via `IOptions<T>`.

**`appsettings.Development.json` additions:**

```json
{
  "DevNotificationDelivery": {
    "Enabled": true,
    "UseSmtp": true,
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "EmailDropFolder": "artifacts/dev-emails",
    "EnableSmsConsoleSink": true
  }
}
```

## 4. Detailed Behavior

### 4.1 Channel Selection

- The delivery service first evaluates user preferences.
- In-app delivery remains handled by the existing notification table and SignalR path.
- Email and SMS are attempted only if the user has enabled that channel for the given event type.

### 4.2 Email Capture Strategy

- Preferred path: send via `DevSmtpEmailChannel` to MailHog or smtp4dev.
- Fallback path: `FileDropEmailChannel` writes the message to disk if SMTP fails or is disabled.
- Delivery logs record which target actually received the message (`DeliveryAttemptRecord`).

### 4.3 SMS Capture Strategy

- `ConsoleSmsChannel` emits a structured log event with a truncated preview.
- The full SMS body is also stored as a `DevSmsMessageRecord` entity.
- Dev-only endpoints allow testers to inspect the captured messages without scanning console output.

### 4.4 Failure Handling

- One channel failing does not block the other channel from executing.
- Each channel returns its own `DeliveryAttemptResult`.
- Failures are surfaced in structured logs and the `DeliveryAttemptRecord` table.

## 5. Acceptance Tests

> **Testing approach:** tests use `ApiWebApplicationFactory` with `DevSmtpEmailChannel` configured to point at a test SMTP server (e.g., netDumbster in-process SMTP or a mock `IEmailChannel`). For SMS, tests assert against `_db.DevSmsMessageRecords` directly.

### 5.1 Email Capture

- Given MailHog is available, when a service reminder notification is delivered with email enabled, then one email appears in MailHog and the delivery log records `Target = smtp`.

**How to verify:** in integration tests, replace `DevSmtpEmailChannel` with a `TestEmailChannel` that captures sent messages in-memory. Assert the captured message contains the expected subject and recipient. Alternatively, use `FileDropEmailChannel` and assert the `.eml` file was created.

### 5.2 File Fallback

- Given SMTP is unavailable, when a notification is delivered with email enabled, then one `.eml` artifact is written to the configured folder and the delivery log records `Target = file-drop`.

**How to verify:** configure `DevNotificationDeliveryOptions.UseSmtp = false`, trigger a notification, then assert `Directory.GetFiles("artifacts/dev-emails", "*.eml").Length == 1`.

### 5.3 SMS Capture

- Given SMS is enabled for a critical alert, when the delivery service runs, then one `DevSmsMessageRecord` is stored and the console logger emits the expected recipient and event type.

**How to verify:** assert `_db.DevSmsMessageRecords.Count() == 1` and verify the `Recipient` and `EventType` fields match expectations.

### 5.4 Preference Enforcement

- Given SMS is disabled for a user, when the notification is delivered, then no SMS attempt is created and the delivery log contains only the enabled channels.

**How to verify:** seed a `NotificationPreference` with `SmsEnabled = false`, dispatch a notification, then assert `_db.DeliveryAttemptRecords.Count(r => r.Channel == "sms") == 0`.

## 6. Security Considerations

- The local delivery channels are registered only in `Development` via the `if (builder.Environment.IsDevelopment())` guard in `Program.cs`.
- Message capture artifacts (`.eml` files) must remain out of source control (add `artifacts/` to `.gitignore`).
- Logs should avoid printing full secrets or authorization links if those links are already persisted elsewhere.
- Dev notification inspection endpoints (`DevNotificationController`) should not be enabled outside local development. Use both `[ApiExplorerSettings(IgnoreApi = true)]` and `IHostEnvironment.IsDevelopment()` runtime guards.

## 7. Multi-Tenancy Considerations

- **`DevSmsMessageRecord` and `DeliveryAttemptRecord`** are development diagnostic entities. They are **not tenant-scoped** — they do not carry `OrganizationId` fields or global query filters. Delivery logs are developer tooling for inspecting notification behavior across all tenants.
- **Notification dispatch** remains tenant-aware through the existing `NotificationDispatchService` which resolves `OrganizationId` from the user record. The channel implementations receive already-rendered content and do not need tenant context.
- **User preferences** are already filtered by user ID within `NotificationDispatchService` (lines 88–92). The `DataSeeder` seeds notification preferences for all Org1 users (Sarah Chen, Mike Rodriguez, James Wilson, Emily Park) with 5 event types each, providing adequate test coverage.

## 8. DI Registration

```csharp
if (builder.Environment.IsDevelopment())
{
    var deliveryConfig = builder.Configuration.GetSection("DevNotificationDelivery");
    var deliveryEnabled = deliveryConfig.GetValue<bool>("Enabled");

    if (deliveryEnabled)
    {
        var useSmtp = deliveryConfig.GetValue<bool>("UseSmtp");
        if (useSmtp)
        {
            builder.Services.AddScoped<IEmailChannel, CompositeEmailChannel>();
            builder.Services.AddScoped<DevSmtpEmailChannel>();
            builder.Services.AddScoped<FileDropEmailChannel>();
        }
        else
        {
            builder.Services.AddScoped<IEmailChannel, FileDropEmailChannel>();
        }

        builder.Services.AddScoped<ISmsChannel, ConsoleSmsChannel>();
        builder.Services.AddScoped<NotificationTemplateRenderer>();
    }
}
```

When `IEmailChannel` and `ISmsChannel` are not registered (production or disabled), the existing `NotificationDispatchService` constructor receives `null` for these optional parameters and falls back to its current log-only behavior.

## 9. Recommended Decomposition

This design covers two independent delivery channels plus their integration with the existing notification service. For incremental delivery:

| Sub-design | Scope | Depends On |
|-----------|-------|------------|
| **20A: Email Channel + Local SMTP** | `IEmailChannel`, `DevSmtpEmailChannel`, `FileDropEmailChannel`, `CompositeEmailChannel`, email delivery logging | — |
| **20B: SMS Channel + Console Sink** | `ISmsChannel`, `ConsoleSmsChannel`, `DevSmsMessageRecord`, SMS inspection via `DevNotificationController` | — |
| **20C: NotificationDispatchService Integration** | Extend existing service with channel injection, `NotificationPreferenceEvaluator`, `NotificationTemplateRenderer`, `DeliveryAttemptRecord` | 20A, 20B |

Email and SMS channels (20A, 20B) can be implemented independently and tested in isolation. The integration step (20C) wires them into the existing notification pipeline.

## 10. Open Questions

1. Should captured email bodies be stored only on disk, or also indexed in SQL for easier search?
2. Should `ConsoleSmsChannel` support a file sink in addition to repository storage for non-database local runs?
3. Is `CompositeEmailChannel` sufficient, or do we want an explicit switch between SMTP and file-drop behavior?
