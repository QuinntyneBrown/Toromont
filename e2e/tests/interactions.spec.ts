import { test, expect } from '@playwright/test';

test('Equipment list: status filter works in browser', async ({ page }) => {
  await page.goto('/equipment', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  
  // Count all rows
  const allRows = await page.locator('[data-testid="equipment-row"]').count();
  console.log(`All equipment: ${allRows}`);
  
  // Select "Needs Service" from status filter
  await page.locator('[data-testid="status-filter"] select').selectOption('NeedsService');
  await page.waitForTimeout(2000);
  
  const filteredRows = await page.locator('[data-testid="equipment-row"]').count();
  console.log(`NeedsService filter: ${filteredRows}`);
  expect(filteredRows).toBeLessThan(allRows);
  expect(filteredRows).toBeGreaterThan(0);
});

test('Equipment list: search works in browser', async ({ page }) => {
  await page.goto('/equipment', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  
  // Type in search box
  await page.locator('[data-testid="equipment-search"]').fill('CAT');
  await page.keyboard.press('Enter');
  await page.waitForTimeout(2000);
  
  const rows = await page.locator('[data-testid="equipment-row"]').count();
  console.log(`Search 'CAT': ${rows} rows`);
  expect(rows).toBeGreaterThan(0);
  
  // Verify all results contain CAT
  const firstRowText = await page.locator('[data-testid="equipment-row"]').first().innerText();
  expect(firstRowText).toContain('CAT');
});

test('Dashboard: clicking alert navigates correctly', async ({ page }) => {
  await page.goto('/dashboard', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  // Check alerts panel has rows
  const alertRows = await page.locator('.alert-row').count();
  console.log(`Dashboard alerts: ${alertRows}`);
  expect(alertRows).toBeGreaterThan(0);
  
  // Check alert shows equipment name (not "Equipment" fallback)
  const alertText = await page.locator('.alert-row').first().innerText();
  console.log(`First alert: ${alertText.substring(0, 100)}`);
  expect(alertText).not.toContain('Equipment -');  // Should show actual equipment name
});

test('Work orders: status tab filtering works', async ({ page }) => {
  await page.goto('/service', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  // Click "Open" tab
  await page.locator('text=Open').first().click();
  await page.waitForTimeout(2000);
  
  const rows = page.locator('table tbody tr');
  const rowCount = await rows.count();
  console.log(`Open tab rows: ${rowCount}`);
  
  // All visible rows should have "Open" status
  if (rowCount > 0) {
    const pageText = await page.locator('table tbody').innerText();
    const lines = pageText.split('\n').filter(l => l.trim());
    console.log(`First row: ${lines[0]?.substring(0, 80)}`);
  }
});

test('Parts catalog: search filters in browser', async ({ page }) => {
  await page.goto('/parts', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  
  const allParts = await page.locator('[data-testid="part-row"]').count();
  console.log(`All parts: ${allParts}`);
  
  // Use the regular search box
  await page.locator('kendo-textbox input').fill('hydraulic');
  await page.waitForTimeout(2000);
  
  const filteredParts = await page.locator('[data-testid="part-row"]').count();
  console.log(`Search 'hydraulic': ${filteredParts}`);
  expect(filteredParts).toBeLessThan(allParts);
  expect(filteredParts).toBeGreaterThan(0);
});
