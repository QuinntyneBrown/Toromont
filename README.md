# Toromont Fleet Hub

A comprehensive fleet management system for Toromont to manage equipment, service operations, parts ordering, telemetry monitoring, AI-driven insights, and notifications/reporting.

## Tech Stack

- **Backend:** ASP.NET Core (.NET 11.0), Entity Framework Core, Serilog, OpenAPI
- **Frontend:** Angular 17, TypeScript, RxJS
- **E2E Testing:** Playwright

## Project Structure

```
src/
├── backend/ToromontFleetHub.Api   # REST API (Controllers, Models, Services, Data)
├── frontend/                      # Angular application
docs/
├── specs/                         # L1 & L2 requirements documentation
├── detailed-designs/              # Technical design documents per feature
├── ui-design.pen                  # UI/UX design file
e2e/                               # Playwright end-to-end tests
```

## Getting Started

### Prerequisites

- [.NET 11.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS recommended)
- [Angular CLI](https://angular.io/cli)

### Backend

```bash
cd src/backend/ToromontFleetHub.Api
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

- **Authentication** — User login and role-based access
- **Equipment Management** — Track and manage fleet equipment
- **Service Management** — Schedule and monitor service operations
- **Parts Ordering** — Order and track parts inventory
- **Telemetry Monitoring** — Real-time equipment telemetry data
- **AI Insights** — AI-driven predictions and anomaly detection
- **Notifications & Reporting** — Alerts, reports, and user preferences
