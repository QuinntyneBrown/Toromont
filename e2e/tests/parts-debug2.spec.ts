import { test } from '@playwright/test';

test('Parts page console errors', async ({ page }) => {
  const errors: string[] = [];
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  page.on('pageerror', err => errors.push(`PAGE: ${err.message}`));
  
  await page.goto('/parts', { waitUntil: 'load' });
  await page.waitForTimeout(5000);
  
  console.log(`Errors: ${errors.length}`);
  errors.forEach(e => console.log(e.substring(0, 250)));
  
  const content = await page.locator('.main-content').innerText();
  console.log(`Content length: ${content.length}`);
  console.log(`Content: "${content.substring(0, 100)}"`);
});
