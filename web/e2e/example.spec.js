import { test, expect } from '@playwright/test';

test.describe('WayPoint Web Portal Smoke Tests', () => {
  test('should load the authentication login view', async ({ page }) => {
    await page.goto('/login');
    await expect(page).toHaveTitle(/WayPoint/i);
    await expect(page.locator('input[type="email"], input[name="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"]')).toBeVisible();
  });

  test('should redirect unauthenticated users from protected dashboard to login', async ({ page }) => {
    await page.goto('/operator');
    await expect(page).toHaveURL(/.*login.*/);
  });
});
