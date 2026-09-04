import { test, expect } from '@playwright/test';

test('WebUI loads successfully', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('body')).toBeVisible();
});
