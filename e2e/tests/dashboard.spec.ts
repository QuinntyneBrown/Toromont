// Acceptance Test
// Traces to: L2-001, L2-002, L2-003
// Description: Verify dashboard KPI cards, equipment map, and active alerts panel

import { test, expect } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';

test.describe('Dashboard', () => {
  let dashboard: DashboardPage;

  test.beforeEach(async ({ page }) => {
    dashboard = new DashboardPage(page);
    await dashboard.goto();
  });

  // L2-001 AC1: Fleet Manager sees KPI cards for total equipment, active, service required, overdue WOs, utilization
  test('displays all five KPI cards with values', async () => {
    await expect(dashboard.kpiTotalEquipment).toBeVisible();
    await expect(dashboard.kpiActiveEquipment).toBeVisible();
    await expect(dashboard.kpiServiceRequired).toBeVisible();
    await expect(dashboard.kpiOverdueWorkOrders).toBeVisible();
    await expect(dashboard.kpiFleetUtilization).toBeVisible();

    const totalValue = await dashboard.getKpiValue('kpi-total-equipment');
    expect(parseInt(totalValue)).toBeGreaterThan(0);
  });

  // L2-001 AC2: KPI cards display trend indicators
  test('KPI cards show trend indicators', async () => {
    const trend = await dashboard.getKpiTrend('kpi-total-equipment');
    expect(trend).toBeTruthy();
  });

  // L2-001 AC4: Desktop layout shows KPI cards in horizontal row
  test('KPI cards display in horizontal row on desktop', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await dashboard.goto();

    await expect(dashboard.kpiRow).toBeVisible();
    const cards = await dashboard.kpiCards.count();
    expect(cards).toBe(5);
  });

  // L2-002 AC1: Equipment map shows markers at GPS positions
  test('equipment map displays with markers', async () => {
    await expect(dashboard.equipmentMap).toBeVisible();
    const markerCount = await dashboard.mapMarkers.count();
    expect(markerCount).toBeGreaterThan(0);
  });

  // L2-002 AC4: Clicking marker shows popup with equipment details
  test('clicking map marker shows equipment popup', async () => {
    await dashboard.clickMapMarker(0);
    const popup = await dashboard.getMarkerPopup();
    await expect(popup).toBeVisible();
    await expect(popup).toContainText(/CAT|Caterpillar/);
  });

  // L2-003 AC1: Alerts panel displays up to 10 alerts ordered by severity
  test('active alerts panel shows alerts ordered by severity', async () => {
    await expect(dashboard.alertsPanel).toBeVisible();
    const alertCount = await dashboard.getAlertCount();
    expect(alertCount).toBeGreaterThan(0);
    expect(alertCount).toBeLessThanOrEqual(10);

    // First alert should be Critical severity
    const firstSeverity = await dashboard.getAlertSeverity(0);
    expect(firstSeverity).toBe('Critical');
  });

  // L2-003 AC3: View All link visible when more than 10 alerts
  test('shows View All link for alerts', async () => {
    await expect(dashboard.viewAllAlertsLink).toBeVisible();
  });
});

// L2-001 AC3: Mobile responsive - KPI cards adapt to 2-column grid
test.describe('Dashboard - Mobile', () => {
  test('KPI cards display in 2-column grid on mobile with 4 cards', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await expect(dashboard.kpiRow).toBeVisible();

    // Mobile shows 4 KPI cards in 2x2 grid (Overdue Work Orders hidden on mobile)
    await expect(dashboard.kpiTotalEquipment).toBeVisible();
    await expect(dashboard.kpiActiveEquipment).toBeVisible();
    await expect(dashboard.kpiServiceRequired).toBeVisible();
    await expect(dashboard.kpiFleetUtilization).toBeVisible();
    await expect(dashboard.kpiOverdueWorkOrders).not.toBeVisible();

    const visibleCards = await dashboard.kpiCards.count();
    expect(visibleCards).toBe(4);
  });

  // Mobile dashboard hides equipment map
  test('equipment map is hidden on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await expect(dashboard.equipmentMap).not.toBeVisible();
  });

  // Mobile dashboard shows alerts panel
  test('alerts panel is visible on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    const dashboard = new DashboardPage(page);
    await dashboard.goto();

    await expect(dashboard.alertsPanel).toBeVisible();
    await expect(dashboard.viewAllAlertsLink).toBeVisible();
  });
});
