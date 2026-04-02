import { test, expect } from '@playwright/test';

test('Debug: Dashboard network and console', async ({ page }) => {
  const logs: string[] = [];
  const networkErrors: string[] = [];
  
  page.on('console', msg => logs.push(`[${msg.type()}] ${msg.text()}`));
  page.on('pageerror', err => logs.push(`[PAGE_ERROR] ${err.message}`));
  page.on('requestfailed', req => networkErrors.push(`FAILED: ${req.url()} - ${req.failure()?.errorText}`));
  
  await page.goto('/dashboard', { waitUntil: 'load' });
  
  // Wait longer
  await page.waitForTimeout(8000);
  
  console.log('\n=== Console logs ===');
  logs.forEach(l => console.log(l.substring(0, 150)));
  
  console.log('\n=== Network failures ===');
  networkErrors.forEach(e => console.log(e));
  
  console.log('\n=== KPI values ===');
  const kpiCards = page.locator('app-kpi-card');
  const count = await kpiCards.count();
  console.log(`KPI cards: ${count}`);
  for (let i = 0; i < count; i++) {
    console.log(`  ${await kpiCards.nth(i).innerText()}`);
  }
  
  const gridRows = page.locator('.alert-row');
  console.log(`\nAlert rows: ${await gridRows.count()}`);
});
