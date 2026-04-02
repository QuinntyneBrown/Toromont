import { test, expect } from '@playwright/test';

test('Network: capture API calls on dashboard', async ({ page }) => {
  const apiCalls: string[] = [];
  
  page.on('request', req => {
    if (req.url().includes('/api/')) {
      apiCalls.push(`>> ${req.method()} ${req.url()}`);
    }
  });
  page.on('response', res => {
    if (res.url().includes('/api/')) {
      apiCalls.push(`<< ${res.status()} ${res.url()}`);
    }
  });
  
  await page.goto('/dashboard', { waitUntil: 'load' });
  await page.waitForTimeout(8000);
  
  console.log(`\nAPI calls captured: ${apiCalls.length}`);
  apiCalls.forEach(c => console.log(`  ${c}`));
  
  if (apiCalls.length === 0) {
    console.log('\n!!! NO API calls were made !!!');
    console.log('This means the component is not making HTTP requests.');
  }
});
