import { test } from '@playwright/test';

test('Parts page debug', async ({ page }) => {
  const errors: string[] = [];
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  page.on('pageerror', err => errors.push(`PAGE_ERROR: ${err.message}`));
  
  await page.goto('/parts', { waitUntil: 'load' });
  await page.waitForTimeout(5000);
  
  console.log('\n=== Page errors ===');
  errors.forEach(e => console.log(e.substring(0, 200)));
  
  const text = await page.locator('.main-content').innerText();
  console.log(`\nPage content (first 200 chars): "${text.substring(0, 200)}"`);
  
  // Check sidebar active
  const sidebarText = await page.locator('app-sidebar').innerText();
  console.log(`\nActive sidebar: ${sidebarText.includes('Parts') ? 'Parts visible' : 'check sidebar'}`);
});
