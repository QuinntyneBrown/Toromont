import { test, expect } from '@playwright/test';

test('User Management with long wait', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', err => errors.push(err.message));
  
  await page.goto('/admin/users', { waitUntil: 'load' });
  
  // Wait up to 15 seconds for content to appear
  try {
    await page.waitForSelector('text=User Management', { timeout: 15000 });
    console.log('Title appeared!');
  } catch {
    console.log('Title never appeared after 15s');
  }
  
  const content = await page.locator('.main-content').innerText();
  console.log(`Content: "${content.substring(0, 150)}"`);
  
  // Check URL - did guard redirect?
  console.log(`Current URL: ${page.url()}`);
  
  if (errors.length) console.log('Errors:', errors.map(e => e.substring(0, 100)));
});
