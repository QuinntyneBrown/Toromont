import { test } from '@playwright/test';

test('AI Insights debug', async ({ page }) => {
  const errors: string[] = [];
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  
  // Intercept API calls
  page.on('response', async res => {
    if (res.url().includes('/ai/')) {
      const body = await res.text().catch(() => 'FAILED');
      console.log(`API ${res.url().split('/api/')[1]}: ${res.status()} body=${body.substring(0, 100)}`);
    }
  });
  
  await page.goto('/ai-insights', { waitUntil: 'networkidle' });
  await page.waitForTimeout(5000);
  
  if (errors.length > 0) console.log('Errors:', errors.join('; ').substring(0, 200));
  
  const predRows = await page.locator('[data-testid="prediction-row"]').count();
  const tableRows = await page.locator('table tbody tr').count();
  console.log(`prediction-row: ${predRows}, table tbody tr: ${tableRows}`);
});
