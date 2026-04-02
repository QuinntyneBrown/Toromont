import { test, expect } from '@playwright/test';

test('Dashboard renders KPI data', async ({ page }) => {
  await page.goto('/dashboard', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  const text = await page.locator('.main-content').innerText();
  expect(text).toContain('Fleet Overview');
  expect(text).not.toContain('TOTAL EQUIPMENT\n0');
  console.log('Dashboard: KPIs render with data ✓');
});

test('Equipment list renders rows', async ({ page }) => {
  await page.goto('/equipment', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  const rows = page.locator('[data-testid="equipment-row"]');
  const count = await rows.count();
  console.log(`Equipment rows: ${count}`);
  expect(count).toBeGreaterThan(0);
  const firstRow = await rows.first().innerText();
  console.log(`First row: ${firstRow.substring(0, 80)}`);
});

test('Work orders renders rows', async ({ page }) => {
  await page.goto('/service', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  // Linter changed to HTML table with data-testid
  const rows = page.locator('table tbody tr');
  const count = await rows.count();
  console.log(`Work order rows: ${count}`);
  expect(count).toBeGreaterThan(0);
});

test('Parts catalog renders rows', async ({ page }) => {
  await page.goto('/parts', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  const rows = page.locator('[data-testid="part-row"]');
  const count = await rows.count();
  console.log(`Parts rows: ${count}`);
  expect(count).toBeGreaterThan(0);
  const text = await page.locator('[data-testid="parts-grid"]').innerText();
  expect(text).toContain('$');
  console.log('Parts: prices visible ✓');
});

test('AI Insights renders predictions', async ({ page }) => {
  await page.goto('/ai-insights', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  const rows = page.locator('[data-testid="prediction-row"]');
  const count = await rows.count();
  console.log(`Prediction rows: ${count}`);
  expect(count).toBeGreaterThan(0);
  const text = await page.locator('[data-testid="predictions-grid"]').innerText();
  expect(text).not.toContain('undefined%');
  console.log('AI: no undefined% values ✓');
});
