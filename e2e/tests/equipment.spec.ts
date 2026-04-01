// Acceptance Test
// Traces to: L2-004, L2-005, L2-006
// Description: Verify equipment list, detail view, and registration

import { test, expect } from '@playwright/test';
import { EquipmentListPage } from '../pages/equipment-list.page';
import { EquipmentDetailPage } from '../pages/equipment-detail.page';

test.describe('Equipment List', () => {
  let equipmentList: EquipmentListPage;

  test.beforeEach(async ({ page }) => {
    equipmentList = new EquipmentListPage(page);
    await equipmentList.goto();
  });

  // L2-004 AC1: Data grid displays with all required columns
  test('displays equipment grid with correct columns', async () => {
    await expect(equipmentList.dataGrid).toBeVisible();
    await expect(equipmentList.pageTitle).toHaveText('Equipment Registry');

    const rowCount = await equipmentList.getRowCount();
    expect(rowCount).toBeGreaterThan(0);
  });

  // L2-004 AC2: Clicking column header sorts the grid
  test('sorts grid when clicking column header', async () => {
    await equipmentList.clickColumnHeader('name');
    const firstName = await equipmentList.getRowData(0, 'name');

    await equipmentList.clickColumnHeader('name');
    const firstNameDesc = await equipmentList.getRowData(0, 'name');

    expect(firstName).not.toBe(firstNameDesc);
  });

  // L2-004 AC3: Status filter shows only matching equipment
  test('filters by status', async () => {
    await equipmentList.selectStatusFilter('NeedsService');

    const rowCount = await equipmentList.getRowCount();
    expect(rowCount).toBeGreaterThan(0);

    // All visible rows should have "Needs Service" or "Service" status
    for (let i = 0; i < rowCount; i++) {
      const status = await equipmentList.getRowData(i, 'status');
      expect(status).toMatch(/Service|Needs Service/i);
    }
  });

  // L2-004 AC4: Search filters by name or serial number
  test('search filters equipment by name', async () => {
    await equipmentList.searchEquipment('CAT 320');

    const rowCount = await equipmentList.getRowCount();
    expect(rowCount).toBeGreaterThan(0);

    const name = await equipmentList.getRowData(0, 'name');
    expect(name).toContain('320');
  });

  // L2-004 AC5: Pagination appears with 20 items per page
  test('shows pagination for large result sets', async () => {
    await expect(equipmentList.pagination).toBeVisible();
    await expect(equipmentList.paginationInfo).toContainText(/Showing.*of/);
  });

  // L2-004 AC6: Mobile view shows card layout
  test('switches to card layout on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await equipmentList.goto();

    // Grid should not be visible, cards should be
    await expect(equipmentList.mobileCards.first()).toBeVisible();
  });
});

test.describe('Equipment Detail', () => {
  let detail: EquipmentDetailPage;

  test.beforeEach(async ({ page }) => {
    detail = new EquipmentDetailPage(page);
    // Navigate to first equipment's detail page
    const list = new EquipmentListPage(page);
    await list.goto();
    await list.clickRow(0);
  });

  // L2-005 AC1: Detail page shows specs, status badge, mini-map
  test('displays equipment specifications and status', async () => {
    await expect(detail.equipmentName).toBeVisible();
    await expect(detail.statusBadge).toBeVisible();
    await expect(detail.specsPanel).toBeVisible();
    await expect(detail.specMake).toBeVisible();
    await expect(detail.specModel).toBeVisible();
    await expect(detail.specYear).toBeVisible();
    await expect(detail.specSerial).toBeVisible();
    await expect(detail.miniMap).toBeVisible();
  });

  // L2-005 AC2: Service history timeline shows recent entries
  test('shows service history timeline', async () => {
    await expect(detail.serviceHistory).toBeVisible();
    const historyCount = await detail.getServiceHistoryCount();
    expect(historyCount).toBeGreaterThan(0);
    expect(historyCount).toBeLessThanOrEqual(5);
  });

  // L2-005 AC3: Telemetry summary cards show latest readings
  test('shows telemetry summary cards', async () => {
    await expect(detail.engineHoursCard).toBeVisible();
    await expect(detail.fuelLevelCard).toBeVisible();
    await expect(detail.temperatureCard).toBeVisible();
  });

  // L2-005 AC4: Fleet Manager sees action buttons
  test('Fleet Manager sees Schedule Service and View Telemetry buttons', async () => {
    await expect(detail.scheduleServiceButton).toBeVisible();
    await expect(detail.viewTelemetryButton).toBeVisible();
  });
});

test.describe('Register New Equipment', () => {
  // L2-006 AC1: Admin/Fleet Manager can access add equipment form
  test('Add Equipment button visible for Fleet Manager', async ({ page }) => {
    const list = new EquipmentListPage(page);
    await list.goto();
    await expect(list.addEquipmentButton).toBeVisible();
  });

  // L2-006 AC2: Valid submission creates equipment and redirects
  test('submitting valid form creates equipment', async ({ page }) => {
    const list = new EquipmentListPage(page);
    await list.goto();
    await list.clickAddEquipment();

    // Fill form
    await page.locator('[data-testid="input-name"]').fill('CAT 336 Test');
    await page.locator('[data-testid="input-make"]').fill('Caterpillar');
    await page.locator('[data-testid="input-model"]').fill('336 NG');
    await page.locator('[data-testid="input-year"]').fill('2025');
    await page.locator('[data-testid="input-serial"]').fill('TEST-SERIAL-001');
    await page.locator('[data-testid="select-category"]').selectOption('Excavator');
    await page.locator('[data-testid="select-status"]').selectOption('Operational');
    await page.locator('[data-testid="submit-equipment"]').click();

    // Should redirect to detail page
    await expect(page).toHaveURL(/\/equipment\/.+/);
    await expect(page.locator('[data-testid="equipment-name"]')).toHaveText('CAT 336 Test');
  });

  // L2-006 AC3: Duplicate serial number shows validation error
  test('duplicate serial number shows error', async ({ page }) => {
    const list = new EquipmentListPage(page);
    await list.goto();
    await list.clickAddEquipment();

    await page.locator('[data-testid="input-name"]').fill('Duplicate Test');
    await page.locator('[data-testid="input-make"]').fill('Caterpillar');
    await page.locator('[data-testid="input-model"]').fill('320 GC');
    await page.locator('[data-testid="input-year"]').fill('2024');
    await page.locator('[data-testid="input-serial"]').fill('ZAP00321'); // Existing serial
    await page.locator('[data-testid="select-category"]').selectOption('Excavator');
    await page.locator('[data-testid="select-status"]').selectOption('Operational');
    await page.locator('[data-testid="submit-equipment"]').click();

    await expect(page.locator('[data-testid="serial-error"]')).toContainText('Serial number already exists');
  });

  // L2-006 AC4: Empty required fields show validation errors
  test('shows validation errors for empty required fields', async ({ page }) => {
    const list = new EquipmentListPage(page);
    await list.goto();
    await list.clickAddEquipment();

    await page.locator('[data-testid="submit-equipment"]').click();

    await expect(page.locator('[data-testid="name-error"]')).toBeVisible();
    await expect(page.locator('[data-testid="serial-error"]')).toBeVisible();
  });
});
