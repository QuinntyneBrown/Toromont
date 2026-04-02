import { test } from '@playwright/test';

test('User Management debug', async ({ page }) => {
  const errors: string[] = [];
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  page.on('pageerror', err => errors.push(`PAGE: ${err.message}`));
  
  await page.goto('/admin/users', { waitUntil: 'load' });
  await page.waitForTimeout(5000);
  
  console.log(`Errors: ${errors.length}`);
  errors.forEach(e => console.log(e.substring(0, 200)));
  
  const content = await page.locator('.main-content').innerText();
  console.log(`\nContent: "${content.substring(0, 100)}"`);
});
