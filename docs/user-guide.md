# Toromont Fleet Hub — User Guide

> **Audience:** End users (non-developers) — Fleet Managers, Technicians, Operators, and Administrators.
>
> This guide walks you through every screen and action available in the Toromont Fleet Hub application so you can manage your equipment fleet, schedule service, order parts, monitor telemetry, review AI insights, and generate reports.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
   - [Signing In](#11-signing-in)
   - [Navigating the Application](#12-navigating-the-application)
2. [Dashboard](#2-dashboard)
   - [KPI Cards](#21-kpi-cards)
   - [Equipment Locations Map](#22-equipment-locations-map)
   - [Active Alerts Panel](#23-active-alerts-panel)
3. [Equipment Management](#3-equipment-management)
   - [Viewing the Equipment List](#31-viewing-the-equipment-list)
   - [Searching and Filtering Equipment](#32-searching-and-filtering-equipment)
   - [Adding New Equipment](#33-adding-new-equipment)
   - [Viewing Equipment Details](#34-viewing-equipment-details)
   - [Editing Equipment](#35-editing-equipment)
4. [Service Management (Work Orders)](#4-service-management-work-orders)
   - [Viewing Work Orders](#41-viewing-work-orders)
   - [Filtering by Status](#42-filtering-by-status)
   - [Creating a Work Order](#43-creating-a-work-order)
   - [Work Order Lifecycle](#44-work-order-lifecycle)
5. [Parts Ordering](#5-parts-ordering)
   - [Browsing the Parts Catalog](#51-browsing-the-parts-catalog)
   - [Filtering and Searching Parts](#52-filtering-and-searching-parts)
   - [Using AI Natural-Language Search](#53-using-ai-natural-language-search)
   - [Adding Parts to Your Cart](#54-adding-parts-to-your-cart)
   - [Managing Your Shopping Cart](#55-managing-your-shopping-cart)
   - [Submitting an Order](#56-submitting-an-order)
6. [Telemetry & Equipment Health](#6-telemetry--equipment-health)
   - [Selecting Equipment](#61-selecting-equipment)
   - [Choosing a Time Range](#62-choosing-a-time-range)
   - [Reading the Charts](#63-reading-the-charts)
7. [AI Insights](#7-ai-insights)
   - [KPI Summary Cards](#71-kpi-summary-cards)
   - [Predictive Maintenance Grid](#72-predictive-maintenance-grid)
   - [Anomaly Alerts Panel](#73-anomaly-alerts-panel)
8. [Reports & Analytics](#8-reports--analytics)
   - [Selecting a Report Type](#81-selecting-a-report-type)
   - [Configuring and Generating a Report](#82-configuring-and-generating-a-report)
   - [Exporting Reports](#83-exporting-reports)
9. [User Management (Administrators Only)](#9-user-management-administrators-only)
   - [Viewing Users](#91-viewing-users)
   - [Inviting a New User](#92-inviting-a-new-user)
   - [Changing a User's Role](#93-changing-a-users-role)
   - [Activating or Deactivating a User](#94-activating-or-deactivating-a-user)
10. [Notifications](#10-notifications)
11. [Navigation & Layout Reference](#11-navigation--layout-reference)
    - [Sidebar](#111-sidebar)
    - [Header Bar](#112-header-bar)
    - [Mobile / Small-Screen Behaviour](#113-mobile--small-screen-behaviour)
12. [User Roles & Permissions](#12-user-roles--permissions)
13. [Glossary](#13-glossary)
14. [Frequently Asked Questions](#14-frequently-asked-questions)

---

## 1. Getting Started

### 1.1 Signing In

Fleet Hub uses your organisation's **Microsoft Entra ID** (formerly Azure Active Directory) account for authentication — the same credentials you use for Outlook or Teams.

| Step | Action |
|------|--------|
| 1 | Open Fleet Hub in your web browser. If you are not already signed in you will see the **Welcome Back** login page. |
| 2 | Click the **Sign in with Microsoft** button. |
| 3 | You will be redirected to the Microsoft login page. Enter your organisational email and password. Complete any multi-factor authentication prompts if required. |
| 4 | After successful authentication you are automatically redirected to the **Dashboard**. |

> **Tip:** If you are already signed in to Microsoft 365 in the same browser, you may be signed in automatically without needing to re-enter your credentials.

### 1.2 Navigating the Application

Once signed in, the application displays three persistent areas:

| Area | Location | Purpose |
|------|----------|---------|
| **Sidebar** | Left edge | Main navigation menu — click any item to switch sections. |
| **Header** | Top bar | Search bar (global search), notification bell, and your user profile avatar. |
| **Content area** | Centre | Shows the page for whichever section you selected in the sidebar. |

The sidebar contains the following navigation items:

| Icon | Label | Goes to |
|------|-------|---------|
| Grid | **Dashboard** | Fleet overview with KPIs and alerts |
| Warehouse | **Equipment** | Equipment registry list |
| Wrench | **Service** | Work order management |
| Package | **Parts** | Parts catalog and ordering |
| Pulse | **Telemetry** | Real-time equipment health charts |
| Brain | **AI Insights** | Predictive maintenance and anomalies |
| Bar chart | **Reports** | Analytics and report generation |
| Settings | **Admin** | User management (visible to Admins only) |

---

## 2. Dashboard

The Dashboard is the first page you see after signing in. It provides a quick snapshot of your fleet's operational health.

### 2.1 KPI Cards

Five metric cards appear across the top of the page:

| Card | What It Shows |
|------|---------------|
| **Total Equipment** | The total number of equipment assets registered in your organisation. |
| **Active** | How many assets are currently in **Operational** status. A green upward-arrow trend indicator means the number is increasing. |
| **Service Required** | Assets that currently need maintenance. A red indicator appears when this number is greater than zero. |
| **Overdue Work Orders** | Work orders that have passed their scheduled date without being completed. A red indicator appears when this number is greater than zero. |
| **Fleet Utilization %** | The percentage of your fleet that is actively operational. |

> The KPI values **refresh automatically every 60 seconds** — no manual action is required.

### 2.2 Equipment Locations Map

Below the KPI cards on the left you will see a map placeholder. This area will display the GPS locations of your equipment on an interactive map once the Leaflet integration is completed.

### 2.3 Active Alerts Panel

On the right side of the Dashboard you will see the **Active Alerts** panel. This shows the most recent alerts generated by the system, sorted by severity.

Each alert displays:
- A **coloured severity dot**: red = Critical, orange = High, yellow = Medium, blue = Low.
- The **equipment name** and a short **alert message**.
- How long ago the alert was raised (e.g. "5 min ago").

Click **View All** at the top-right of the panel to see the full alerts list.

If there are no current alerts, the panel displays the message *"No active alerts"*.

---

## 3. Equipment Management

### 3.1 Viewing the Equipment List

Navigate to **Equipment** in the sidebar. You will see the **Equipment Registry** page with a data grid containing all equipment in your organisation.

The grid columns are:

| Column | Description |
|--------|-------------|
| **Serial #** | The unique serial number of the equipment (clickable — opens detail view). |
| **Name** | The make and model combined. |
| **Model** | Equipment model designation. |
| **Serial Number** | Repeated serial number for quick reference. |
| **Category** | Equipment type (e.g. Excavator, Loader, Dozer, Crane, Truck, Generator, Compressor). |
| **Status** | A colour-coded badge — **Operational** (green), **Needs Service** (orange), **Out of Service** (red). |
| **Location** | Latitude and longitude coordinates. |
| **Hours** | Total engine hours recorded. |

- **20 items** are shown per page. Use the pagination controls at the bottom to move between pages.
- Click any **column header** to sort the list in ascending or descending order.

### 3.2 Searching and Filtering Equipment

Three filter controls sit above the grid:

| Control | How to Use It |
|---------|---------------|
| **Status dropdown** | Select a status (All, Active, In Service, Down, Retired) to show only equipment with that status. |
| **Category dropdown** | Select a category (All, Excavator, Loader, Dozer, Crane, Truck, Generator, Compressor, Other) to narrow the list. |
| **Search box** | Type part of an equipment name or serial number. The list updates automatically after a short pause (about half a second). Clear the search box to see all equipment again. |

Filters and search may be combined — for example, you can filter by *Category = Loader* and *Status = Active* at the same time.

### 3.3 Adding New Equipment

> **Required role:** Admin or Fleet Manager.

1. On the Equipment Registry page, click the **+ Add Equipment** button in the top-right corner.
2. A dialog appears with the following fields:

   | Field | Description |
   |-------|-------------|
   | **Make** | Manufacturer name (e.g. Caterpillar, Komatsu). |
   | **Model** | Model designation (e.g. 320F, WA380). |
   | **Serial Number** | The unique serial number for this asset. |
   | **Year** | Year of manufacture (range 1990–2030). |
   | **Category** | Select from the dropdown (Excavator, Loader, Dozer, etc.). |

3. Click **Save** to create the equipment record. The dialog closes and the new asset appears in the list.
4. Click **Cancel** to close the dialog without saving.

### 3.4 Viewing Equipment Details

Click any equipment name or serial number link in the grid. You will be taken to the **Equipment Detail** page, which shows:

**Specifications card:**
- Make, Model, Year, Serial Number, Category, and Engine Type.

**Telemetry cards** (three small KPI cards):
- **Engine Hours** — Total hours of operation.
- **Fuel Level** — Current fuel level as a percentage.
- **Temperature** — Current operating temperature.

**Location map:**
- A map showing the equipment's last known GPS position.

**Service History timeline:**
- A chronological list of work orders associated with this equipment.
- Each entry shows a coloured priority dot, the work order title, and the date.
- If no work orders exist yet, the message *"No service history"* is displayed.

**Action buttons** at the top of the page:

| Button | Action |
|--------|--------|
| **Schedule Service** | Navigates to the Service page to create a new work order for this equipment. |
| **View Telemetry** | Navigates to the Telemetry dashboard, pre-filtered to this equipment. |
| **Edit** | Opens the equipment for editing. |

Use the **breadcrumb** link at the top (Equipment > [Name]) to navigate back to the equipment list.

### 3.5 Editing Equipment

> **Required role:** Admin or Fleet Manager.

On the Equipment Detail page, click the **Edit** button. Update any fields as needed and save your changes. The updated information will appear immediately.

---

## 4. Service Management (Work Orders)

Navigate to **Service** in the sidebar to open the **Service Management** page.

### 4.1 Viewing Work Orders

The page displays a data grid of all work orders. The grid columns are:

| Column | Description |
|--------|-------------|
| **WO #** | A short identifier for the work order. |
| **Title** | The work order title. |
| **Equipment** | The name of the associated equipment (clickable — navigates to equipment detail). |
| **Service Type** | A brief description of the service to be performed. |
| **Priority** | A colour-coded badge — **Critical** (red), **High** (orange), **Medium** (blue), **Low** (blue). |
| **Status** | A colour-coded badge — **Open** (blue), **In Progress** (orange), **Completed** (green), **Cancelled** (red). |
| **Scheduled** | The scheduled date for the service, or *"Unscheduled"* if no date has been set. |
| **Assigned To** | The name of the assigned technician, or *"Unassigned"*. |

Work orders display **20 items per page** with pagination controls.

### 4.2 Filtering by Status

Above the grid is a row of **status tabs**:

| Tab | Shows |
|-----|-------|
| **All** | Every work order regardless of status. |
| **Open** | Only work orders with Open status. |
| **In Progress** | Only work orders currently being worked on. |
| **On Hold** | Work orders that have been paused. |
| **Completed** | Finished work orders. |
| **Closed** | Formally closed work orders. |

Each tab displays a **count badge** showing how many work orders are in that status. Click a tab to filter the grid instantly.

### 4.3 Creating a Work Order

> **Required role:** Fleet Manager or Technician.

1. Click the **+ Create Work Order** button in the top-right corner.
2. A dialog appears with the following fields:

   | Field | Description |
   |-------|-------------|
   | **Equipment** | Select the equipment from the dropdown. You can type to search/filter the list. |
   | **Title** | A short descriptive title for the work order. |
   | **Service Type / Description** | A longer description of the work to be done. |
   | **Priority** | Select from the dropdown: Low, Medium (default), High, or Critical. |
   | **Scheduled Date** | Use the date picker to choose when the service should be performed. |
   | **Assigned Technician** | Optionally enter the name of the technician to assign. |

3. Click **Create** to submit. The dialog closes and the new work order appears in the list. Tab counts update automatically.
4. Click **Cancel** to close without creating.

### 4.4 Work Order Lifecycle

Work orders move through the following statuses:

```
Open → In Progress → On Hold → Completed → Closed
                                    ↘ Cancelled
```

- When a work order status changes, the system records who made the change, when, and any notes provided.
- Administrators and Fleet Managers can reopen or cancel work orders.
- Technicians can move work orders from Open to In Progress, to Completed.

---

## 5. Parts Ordering

Navigate to **Parts** in the sidebar to open the **Parts Catalog**.

### 5.1 Browsing the Parts Catalog

The Parts Catalog page is divided into two areas:

- **Filters sidebar** (left side on desktop; toggle button on mobile).
- **Parts grid** (main content area).

The grid columns are:

| Column | Description |
|--------|-------------|
| **Part #** | The unique part number. |
| **Name** | The part name. |
| **Description** | A brief description (truncated to keep the table compact). |
| **Price** | Unit price in USD (e.g. $123.45). |
| **Availability** | A colour-coded badge — **In Stock** (green), **Low Stock** (orange), **Out of Stock** (red). |
| **Compatible Models** | Which equipment models this part fits, or *"Universal"* if it fits all. |
| **Add to Cart** | A button to add one unit to your shopping cart. Disabled for out-of-stock parts. |

The catalog shows **20 items per page** with pagination controls.

### 5.2 Filtering and Searching Parts

**Category filters** (sidebar checkboxes):
Check one or more categories to narrow the catalog:
- Filters, Hydraulics, Electrical, Engine, Transmission, Undercarriage, Cab, Other.

**Availability dropdown:**
Select *All*, *In Stock*, *Low Stock*, or *Out of Stock*.

**Text search box:**
Type a part number or name — results update automatically after a brief pause.

**Clear Filters button:**
Click to reset all filters and return to the full catalog.

### 5.3 Using AI Natural-Language Search

At the top of the parts grid is an **AI Search** bar (marked with a star icon).

| Step | Action |
|------|--------|
| 1 | Type a search query in plain English — for example, *"engine air filters for CAT loaders"* or *"hydraulic hoses compatible with 320F"*. |
| 2 | Click the **Search** button (or press Enter). |
| 3 | The catalog updates to show the most relevant parts matching your query (up to 50 results). |

> **Tip:** AI search understands natural language, so you do not need to know exact part numbers. Describe what you need in your own words.

### 5.4 Adding Parts to Your Cart

1. Find the part you want in the catalog.
2. Click the **Add to Cart** button in that row.
3. One unit of the part is added to your shopping cart.

> **Note:** The *Add to Cart* button is **disabled** (greyed out) for parts that are **Out of Stock**.

To view your cart, navigate to **Parts → Cart** or click the cart link.

### 5.5 Managing Your Shopping Cart

Navigate to the **Shopping Cart** page (accessible from the Parts section).

**If the cart is empty**, you will see:
- A shopping cart icon.
- The message *"Your cart is empty"*.
- A **Browse Parts** button to return to the catalog.

**If items are in the cart**, you will see a table:

| Column | Description |
|--------|-------------|
| **Part** | Part name with part number shown below in grey. |
| **Unit Price** | The price per unit. |
| **Quantity** | A numeric input — increase or decrease the quantity (minimum 1, maximum 999). Changes save automatically. |
| **Line Total** | Unit price × quantity, calculated automatically. |
| **Remove (×)** | Click to remove the item from your cart. |

On the right side of the page a **summary panel** shows:
- **Subtotal** (number of items) — total cost before any additional fees.
- **Total** — the final amount (shown in bold).
- A **Submit Order** button.

### 5.6 Submitting an Order

> **Required role:** Fleet Manager or Technician.

1. Review the items and quantities in your cart.
2. Click **Submit Order**.
3. The button text changes to *"Submitting…"* while the order is being processed.
4. On success, a **confirmation dialog** appears:
   - ✅ *"Order Submitted Successfully"*
   - *"Your order has been placed and is being processed."*
5. Click **OK** — the cart is cleared and you are returned to the Parts Catalog.

---

## 6. Telemetry & Equipment Health

Navigate to **Telemetry** in the sidebar.

### 6.1 Selecting Equipment

At the top of the page, use the **Equipment dropdown** to choose which asset you want to monitor. The dropdown shows each equipment in the format *Make Model (Serial Number)* and is searchable — type to filter the list.

If you arrived from an Equipment Detail page, the equipment is pre-selected for you.

### 6.2 Choosing a Time Range

Four time-range buttons appear next to the dropdown:

| Button | Shows data from |
|--------|-----------------|
| **24h** | The last 24 hours |
| **7d** | The last 7 days (default) |
| **30d** | The last 30 days |
| **90d** | The last 90 days |

Click a button to reload the charts with data for that period. The currently selected button is highlighted.

A **"Last updated"** timestamp shows when the data was most recently fetched.

### 6.3 Reading the Charts

Four chart panels are displayed in a 2 × 2 grid:

| Chart | Type | What It Shows |
|-------|------|---------------|
| **Engine Hours** | Line chart | Cumulative engine hours over the selected time period. Hover over data points for exact values. |
| **Fuel Consumption** | Bar chart | Fuel consumed (in litres) during each time interval. Hover for exact values. |
| **Temperature** | Line chart | Operating temperature (°C) over time. Hover for exact values. |
| **GPS Trail** | Map | A map tracing the equipment's movements (coming soon). |

> The charts **auto-refresh every 60 seconds**. If no equipment is selected, the message *"Select equipment to view telemetry data"* appears in each panel.

---

## 7. AI Insights

Navigate to **AI Insights** in the sidebar.

### 7.1 KPI Summary Cards

Four cards appear at the top:

| Card | What It Shows | Colour |
|------|---------------|--------|
| **Total Predictions** | Number of active (non-dismissed) maintenance predictions. | White |
| **High Priority** | Predictions with a confidence score of 80 % or higher. | Red highlight |
| **Active Anomalies** | Anomalies detected in the last 30 days. | Orange highlight |
| **Est. Cost Savings** | Estimated savings from acting on predictions (calculated as prediction count × $2,500). | Green highlight |

### 7.2 Predictive Maintenance Grid

The left side of the page contains a grid of AI-generated predictions:

| Column | Description |
|--------|-------------|
| **Equipment** | The asset the prediction relates to. |
| **Component** | The specific component at risk (e.g. Hydraulic Pump, Engine Belt). |
| **Confidence** | A progress bar showing the AI confidence level (0–100 %). Red if ≥ 80 %, orange if 50–79 %, grey if below 50 %. |
| **Recommended Action** | What the AI suggests you do (e.g. "Replace hydraulic seals"). |
| **Timeframe** | How soon action is recommended (e.g. "Within 2 weeks"). |
| **Priority** | A badge — *High Priority* (red), *Medium* (orange), or *Low Confidence* (blue). |
| **Actions** | A **Dismiss** button to remove the prediction from the list. |

The grid shows **10 items per page** with pagination.

**To dismiss a prediction:** Click the **Dismiss** button. The row is removed, the KPI counts update, and the estimated cost savings recalculate.

### 7.3 Anomaly Alerts Panel

The right side of the page shows a scrollable panel of recently detected anomalies (last 30 days).

Each anomaly card shows:
- A **colour-coded icon** indicating severity: red (Critical), orange (High), yellow (Medium), grey (Low).
- The **parameter name** being monitored (e.g. Oil Pressure, Coolant Temperature).
- **Expected** vs. **Actual** values.
- The **deviation percentage** — displayed in red if the deviation exceeds 20 %, or in orange/yellow otherwise.
- **Time ago** since detection (e.g. *"5 min ago"*).

If no anomalies are present the message *"No anomalies detected"* appears.

---

## 8. Reports & Analytics

Navigate to **Reports** in the sidebar.

> **Required role:** Fleet Manager or Admin.

### 8.1 Selecting a Report Type

Three report-type cards are displayed:

| Card | Icon | Description |
|------|------|-------------|
| **Fleet Utilization** | Bar chart | Shows how much each piece of equipment is being used (hours). |
| **Maintenance Costs** | Dollar sign | Shows maintenance spending across equipment. |
| **Equipment Lifecycle** | Cycle arrows | Shows the lifecycle value and breakdown of equipment. |

Click a card to select it. The selected card is highlighted with a yellow border and icon background.

### 8.2 Configuring and Generating a Report

Below the report cards is a **configuration toolbar**:

| Control | Description |
|---------|-------------|
| **Start Date** | Use the date picker to set the beginning of the reporting period. Defaults to 3 months ago. |
| **End Date** | Use the date picker to set the end of the reporting period. Defaults to today. |
| **Equipment** | Select a specific equipment from the dropdown, or leave as *"All Equipment"* for a fleet-wide report. |
| **Generate Report** | Click to generate the report. |

After clicking **Generate Report**, two charts appear:

- **Bar chart** (left, larger): Shows values by equipment or category. The Y-axis label changes depending on report type (Hours, Cost ($), or Value).
- **Pie chart** (right, smaller): Shows the proportional breakdown, with a legend at the bottom.

If no report has been generated yet, you will see the message *"Select a report type and click 'Generate Report' to view charts"*.

### 8.3 Exporting Reports

Three export buttons appear next to the Generate Report button:

| Button | Format | File Downloaded |
|--------|--------|-----------------|
| **PDF** | Adobe PDF | `fleet-utilization-report.pdf` (name varies by report type) |
| **Excel** | Microsoft Excel (.xlsx) | `maintenance-costs-report.xlsx` |
| **CSV** | Comma-Separated Values | `equipment-lifecycle-report.csv` |

Click the desired export button. The file is generated on the server and downloaded through your browser's standard download dialog.

---

## 9. User Management (Administrators Only)

Navigate to **Admin** in the sidebar.

> **Required role:** Admin. This section is not accessible to other roles.

### 9.1 Viewing Users

The page shows a grid of all users in your organisation:

| Column | Description |
|--------|-------------|
| **Name** | User's full name with an avatar showing their initials. |
| **Email** | The user's email address. |
| **Role** | A dropdown showing the user's current role (see [User Roles](#12-user-roles--permissions)). |
| **Status** | A badge — **Active** (green) or **Inactive** (red). |
| **Last Login** | The date of the user's most recent login, or *"Never"* if they haven't signed in yet. |
| **Actions** | A button to **Deactivate** (or **Activate** if currently inactive). |

The grid shows **15 users per page** with pagination.

### 9.2 Inviting a New User

1. Click the **+ Invite User** button in the top-right corner.
2. A dialog appears with:

   | Field | Description |
   |-------|-------------|
   | **Email Address** | Enter the new user's email (e.g. user@company.com). |
   | **Role** | Select the role to assign: Admin, Fleet Manager, Technician, Parts Specialist, or Read Only. |

3. Click **Send Invite**. An invitation email is sent to the user with a link to set up their account.
4. The dialog closes and the user list refreshes — the new user appears with a *"Never"* last login.

> **Note:** The **Send Invite** button is disabled until you enter an email address.

### 9.3 Changing a User's Role

1. In the users grid, find the user whose role you want to change.
2. Click the **Role** dropdown in their row.
3. Select the new role. The change is saved automatically.

> **Note:** You cannot change your own role.

### 9.4 Activating or Deactivating a User

1. In the users grid, find the user.
2. Click the **Deactivate** button (or **Activate** if the user is currently inactive).
3. The user's status badge updates immediately.

Deactivated users cannot sign in to Fleet Hub until they are reactivated.

> **Note:** You cannot deactivate your own account.

---

## 10. Notifications

Fleet Hub delivers real-time notifications to keep you informed about important events.

**Notification bell** — In the header bar (top-right), you will see a bell icon. A red badge with a number indicates how many **unread notifications** you have.

Notifications may be triggered by events such as:
- A critical equipment alert is raised.
- A work order assigned to you changes status.
- A parts order you submitted is updated.
- An AI prediction requires attention.

Notifications are delivered in real time — they appear as soon as the event occurs, without needing to refresh the page.

---

## 11. Navigation & Layout Reference

### 11.1 Sidebar

The sidebar is always visible on the left edge of the screen. It contains:
- The **Fleet Hub** logo and branding at the top.
- Navigation links to each section (Dashboard, Equipment, Service, Parts, Telemetry, AI Insights, Reports, Admin).
- The currently active page is highlighted with a golden/yellow accent.

On desktop you can **collapse** the sidebar to show only icons (saving screen space). Click the collapse toggle near the logo area. The sidebar smoothly transitions between expanded (240 px) and collapsed (64 px) widths.

### 11.2 Header Bar

The header bar runs across the top of every page and contains:
- **Search bar** (left): Type to search across equipment, orders, and alerts.
- **Notification bell** (right): Shows unread notification count.
- **User avatar and name** (far right): Displays your initials and full name.

### 11.3 Mobile / Small-Screen Behaviour

Fleet Hub is fully responsive. On smaller screens (phones and small tablets):

| Feature | Behaviour on mobile |
|---------|---------------------|
| **Sidebar** | Collapses to icon-only view (64 px wide). Text labels are hidden. |
| **Equipment list** | Switches from a data grid to a **card layout** — each equipment is shown as an individual card. |
| **Parts catalog filters** | The filter sidebar is hidden. A **Show Filters / Hide Filters** toggle button appears above the grid. |
| **Charts and grids** | Stack vertically instead of side-by-side. |
| **Data tables** | Become horizontally scrollable. |

---

## 12. User Roles & Permissions

Fleet Hub uses **role-based access control**. Your administrator assigns you one of the following roles:

| Role | What You Can Do |
|------|-----------------|
| **Admin** | Full access to all features. Manage users (invite, change roles, activate/deactivate). Access all equipment, work orders, parts, telemetry, AI insights, and reports. |
| **Fleet Manager** | Create, edit, and delete equipment. Create and manage work orders (including reopen/cancel). Order parts. View telemetry and AI insights. Generate and export reports. |
| **Technician** | View equipment (read-only). Create and update work orders. Order parts. View telemetry and AI insights. |
| **Viewer / Operator** | Read-only access to equipment, work orders, telemetry, and AI insights. Cannot create or modify records. Cannot order parts. |

### Quick Permissions Reference

| Action | Admin | Fleet Manager | Technician | Viewer |
|--------|:-----:|:-------------:|:----------:|:------:|
| View Dashboard | ✅ | ✅ | ✅ | ✅ |
| View Equipment | ✅ | ✅ | ✅ | ✅ |
| Add / Edit / Delete Equipment | ✅ | ✅ | ❌ | ❌ |
| View Work Orders | ✅ | ✅ | ✅ | ✅ |
| Create Work Orders | ✅ | ✅ | ✅ | ❌ |
| Close / Cancel Work Orders | ✅ | ✅ | ❌ | ❌ |
| Browse Parts Catalog | ✅ | ✅ | ✅ | ✅ |
| Order Parts | ✅ | ✅ | ✅ | ❌ |
| View Telemetry | ✅ | ✅ | ✅ | ✅ |
| View AI Insights | ✅ | ✅ | ✅ | ✅ |
| Generate Reports | ✅ | ✅ | ❌ | ❌ |
| Export Reports (PDF/Excel/CSV) | ✅ | ✅ | ❌ | ❌ |
| Manage Users | ✅ | ❌ | ❌ | ❌ |

---

## 13. Glossary

| Term | Definition |
|------|------------|
| **Asset / Equipment** | A piece of heavy equipment (excavator, loader, dozer, crane, truck, generator, or compressor) tracked by the system. |
| **Work Order** | A service request or maintenance ticket associated with a specific equipment asset. |
| **Telemetry** | Real-time sensor data collected from equipment, including engine hours, fuel level, temperature, and GPS location. |
| **AI Prediction** | A system-generated forecast indicating that a specific equipment component may need maintenance within a given timeframe. |
| **Anomaly** | A telemetry reading that deviates significantly from expected values, potentially indicating a problem. |
| **KPI** | Key Performance Indicator — a summary metric displayed on dashboards. |
| **Entra ID** | Microsoft's identity platform (formerly Azure Active Directory) used for sign-in. |
| **Role** | Your assigned permission level (Admin, Fleet Manager, Technician, or Viewer). |
| **Organisation / Tenant** | Your company's isolated workspace within Fleet Hub. Each organisation's data is completely separate. |
| **Confidence Score** | A percentage (0–100 %) representing how certain the AI is about a prediction. Higher values indicate greater certainty. |

---

## 14. Frequently Asked Questions

**Q: I can't see the Admin section in the sidebar.**
A: The Admin section is only visible to users with the **Admin** role. Contact your organisation's administrator if you believe you should have access.

**Q: The "Add to Cart" button is greyed out for a part I need.**
A: This means the part is currently **Out of Stock**. Check back later or contact your Toromont representative for availability.

**Q: How often does the dashboard data update?**
A: Dashboard KPIs and the telemetry charts refresh automatically every **60 seconds**. You do not need to reload the page.

**Q: Can I order parts for equipment in another organisation?**
A: No. Fleet Hub enforces data isolation between organisations. You can only view and manage equipment and orders within your own organisation.

**Q: How do I reset my password?**
A: Fleet Hub uses your Microsoft Entra ID credentials. To reset your password, use the Microsoft self-service password reset portal or contact your IT department.

**Q: What does "Dismiss" do on an AI prediction?**
A: Dismissing a prediction removes it from the active list and decreases the KPI counts. It does not delete the prediction — it marks it as reviewed so it no longer requires your attention.

**Q: How do I export a report?**
A: On the Reports page, first generate a report by selecting a report type and clicking **Generate Report**. Then click the **PDF**, **Excel**, or **CSV** button to download the report in your preferred format.

**Q: What browsers are supported?**
A: Fleet Hub works best in modern browsers such as Microsoft Edge, Google Chrome, Mozilla Firefox, and Safari. For the best experience, keep your browser up to date.

**Q: Who do I contact for support?**
A: Reach out to your organisation's Fleet Hub administrator or contact Toromont support for technical assistance.
