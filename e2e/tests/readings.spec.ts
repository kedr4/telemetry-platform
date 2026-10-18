import { expect, test } from '@playwright/test';
import { expectNoSeriousA11yViolations } from '../support/accessibility';
import { locations } from '../support/gateway';

test.describe('Readings page', () => {
  test('is reachable from the main navigation', async ({ page }) => {
    await page.goto('/');

    await page
      .getByRole('navigation', { name: 'Main' })
      .getByRole('link', { name: 'Readings' })
      .click();

    await expect(page).toHaveURL(/\/readings$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Readings' })).toBeVisible();
  });

  test('pages through stored readings', async ({ page }) => {
    await page.goto('/readings');
    const range = page.getByRole('navigation', { name: 'Pagination' }).locator('.range');
    await expect(range).toHaveText(/^1–\d+ of \d+$/);
    const firstPageTopRow = await page.locator('tr.mat-mdc-row').first().textContent();

    await page.getByRole('button', { name: 'Next page' }).click();
    await expect(range).toHaveText(/^\d+–\d+ of \d+$/);
    await expect(range).not.toHaveText(/^1–/);
    await expect(page.locator('tr.mat-mdc-row').first()).not.toHaveText(firstPageTopRow ?? '');

    await page.getByRole('button', { name: 'Previous page' }).click();
    await expect(range).toHaveText(/^1–\d+ of \d+$/);
    await expect(page.getByRole('button', { name: 'Previous page' })).toBeDisabled();
  });

  test('filters by room', async ({ page, request }) => {
    const [room] = await locations(request);
    await page.goto('/readings');
    await expect(page.locator('tr.mat-mdc-row').first()).toBeVisible();

    await page.getByRole('combobox', { name: 'Rooms' }).click();
    await page.getByRole('option', { name: room!, exact: true }).click();
    await page.keyboard.press('Escape');

    const roomCells = page.locator('tr.mat-mdc-row td.mat-column-location');
    await expect(roomCells.first()).toHaveText(room!);
    for (const cell of await roomCells.all()) {
      await expect(cell).toHaveText(room!);
    }

    await page.getByRole('button', { name: 'Clear' }).click();
    await expect(page.getByRole('combobox', { name: 'Rooms' })).not.toContainText(room!);
  });

  test('has no serious accessibility violations', async ({ page }) => {
    await page.goto('/readings');
    await expect(page.locator('tr.mat-mdc-row').first()).toBeVisible();
    await expectNoSeriousA11yViolations(page);
  });
});
