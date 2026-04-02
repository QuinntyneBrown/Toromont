import { test, expect } from '@playwright/test';

test('Dashboard shows correct KPI values', async ({ page }) => {
  const errors: string[] = [];
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  
  await page.goto('/dashboard', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  // Check KPI cards rendered with values
  const kpiCards = page.locator('app-kpi-card');
  const count = await kpiCards.count();
  console.log(`KPI cards found: ${count}`);
  expect(count).toBe(5);
  
  // Get text content of all KPI cards
  for (let i = 0; i < count; i++) {
    const text = await kpiCards.nth(i).innerText();
    console.log(`  KPI ${i}: "${text.replace(/\n/g, ' | ')}"`);
  }
  
  // Check alerts section
  const alertRows = page.locator('.alert-row');
  const alertCount = await alertRows.count();
  console.log(`Alert rows: ${alertCount}`);
  
  if (errors.length > 0) {
    console.log('Console errors:', errors);
  }
});

test('Equipment list shows data rows', async ({ page }) => {
  await page.goto('/equipment', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  // Check grid has rows
  const gridRows = page.locator('kendo-grid tbody tr');
  const rowCount = await gridRows.count();
  console.log(`Equipment grid rows: ${rowCount}`);
  expect(rowCount).toBeGreaterThan(0);
  
  // Check first row has actual data (not empty cells)
  const firstRowText = await gridRows.first().innerText();
  console.log(`First row: "${firstRowText.substring(0, 100)}"`);
  expect(firstRowText.trim().length).toBeGreaterThan(10);
});

test('Work orders grid shows data', async ({ page }) => {
  await page.goto('/service', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  const gridRows = page.locator('kendo-grid tbody tr');
  const rowCount = await gridRows.count();
  console.log(`Work order grid rows: ${rowCount}`);
  expect(rowCount).toBeGreaterThan(0);
  
  // Check WO number format (should be WO-YYYY-NNN, not a GUID)
  const firstCell = await gridRows.first().locator('td').first().innerText();
  console.log(`First WO cell: "${firstCell.trim()}"`);
  expect(firstCell.trim()).toMatch(/WO-\d{4}-\d{3}/);
});

test('Parts catalog shows parts with prices', async ({ page }) => {
  await page.goto('/parts', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  const gridRows = page.locator('kendo-grid tbody tr');
  const rowCount = await gridRows.count();
  console.log(`Parts grid rows: ${rowCount}`);
  expect(rowCount).toBeGreaterThan(0);
  
  // Check a price cell contains a dollar amount
  const pageText = await page.locator('kendo-grid').innerText();
  const hasPrices = pageText.includes('$');
  console.log(`Has prices: ${hasPrices}`);
  expect(hasPrices).toBe(true);
});

test('AI Insights shows predictions', async ({ page }) => {
  await page.goto('/ai-insights', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  const gridRows = page.locator('kendo-grid tbody tr');
  const rowCount = await gridRows.count();
  console.log(`AI predictions grid rows: ${rowCount}`);
  expect(rowCount).toBeGreaterThan(0);
  
  // Check confidence percentages are visible (not "undefined%")
  const pageText = await page.locator('kendo-grid').innerText();
  const hasUndefined = pageText.includes('undefined');
  console.log(`Has 'undefined': ${hasUndefined}`);
  expect(hasUndefined).toBe(false);
});
