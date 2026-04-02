# Ironvale Fleet Hub

A comprehensive, multi-tenant fleet management system for Ironvale to manage equipment, service operations, parts ordering, telemetry monitoring, AI-driven insights, and notifications/reporting.

## Tech Stack

- **Backend:** ASP.NET Core (.NET 9.0), Entity Framework Core, MediatR (CQRS), FluentValidation, Serilog, SignalR, Dapper, OpenAPI
- **Frontend:** Angular 21, TypeScript, Kendo UI, Bootstrap 5, RxJS, Leaflet, MSAL (Azure AD)
- **Infrastructure:** Azure (App Service, Functions, Logic Apps, API Management, OpenAI, Communication Services)
- **Data:** SQL Server 2022, EF Core migrations, time-series telemetry with 90-day retention
- **Testing:** Playwright (E2E), Karma/Jasmine (unit)

## Project Structure

```
src/
├── backend/IronvaleFleetHub.Api   # REST API (Controllers, Models, Services, DTOs, Hubs, Middleware)
├── frontend/                      # Angular application (standalone components)
docs/
├── specs/                         # L1 & L2 requirements documentation
├── detailed-designs/              # Technical design documents per feature (7 modules)
├── adrs/                          # Architecture Decision Records (35 ADRs)
designs/
├── ui-design.pen                  # UI/UX design file
├── exports/                       # Exported design assets (PNG)
e2e/                               # Playwright end-to-end tests (9 spec files)
Ironvale.sln                       # Visual Studio solution
```

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS recommended)
- [Angular CLI](https://angular.dev/tools/cli)

### Backend

```bash
cd src/backend/IronvaleFleetHub.Api
dotnet restore
dotnet run
```

### Frontend

```bash
cd src/frontend
npm install
ng serve
```

The frontend will be available at `http://localhost:4200`.

### E2E Tests

```bash
cd e2e
npm install
npx playwright test
```

## Feature Modules

- **Dashboard** — Overview of fleet status and key metrics
- **Authentication** — Azure AD/Entra ID login with multi-tenant RBAC
- **Equipment Management** — Track and manage fleet equipment
- **Service Management** — Schedule and monitor work orders
- **Parts Ordering** — Browse catalog, manage cart, and track orders
- **Telemetry Monitoring** — Real-time equipment telemetry with map visualization
- **AI Insights** — Azure OpenAI-driven predictions and anomaly detection
- **Notifications & Reporting** — Real-time alerts via SignalR, PDF/Excel/CSV report export
- **User Management** — Multi-org user administration and preferences
