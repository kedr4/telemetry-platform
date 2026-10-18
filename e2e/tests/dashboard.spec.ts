import { expect, test } from '@playwright/test';
import { expectNoSeriousA11yViolations } from '../support/accessibility';
import { locations } from '../support/gateway';

test.describe('Overview dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('connects to live updates', async ({ page }) => {
    await expect(page.getByRole('status')).toContainText('Live');
  });

  test('shows a card with the latest values of every room', async ({ page, request }) => {
    const rooms = await locations(request);
    const current = page.locator('app-panel').filter({ hasText: 'Current readings' });

    for (const room of rooms) {
      const card = current.locator('app-room-card').filter({
        has: page.getByText(room, { exact: true }),
      });
      await expect(card).toBeVisible();
      await expect(card).toContainText('Energy');
      await expect(card).toContainText('CO₂');
    }
  });

  test('receives newly stored batches over SignalR', async ({ page }) => {
    const feed = page.locator('app-live-feed');
    await expect(feed).toContainText('Waiting for live readings');

    // The ingestor polls every few seconds, but the upstream API fails often by design.
    await expect(feed.locator('li', { hasText: /\d+ readings/ }).first()).toBeVisible({
      timeout: 60_000,
    });
    await expect(page.getByRole('status')).toContainText(/Live\s*·\s*\d{2}:\d{2}:\d{2}/);
  });

  test('renders the charts', async ({ page }) => {
    for (const heading of ['Trends', 'Rooms compared', 'Readings by meter type']) {
      const panel = page.locator('app-panel').filter({ hasText: heading });
      await expect(panel.locator('app-chart canvas').first()).toBeVisible();
    }
  });

  test('switches the time range of every chart', async ({ page }) => {
    const trends = page.locator('app-panel').filter({ hasText: 'Trends' });
    await expect(trends).toContainText('last 1 h');

    await page.getByRole('radio', { name: '24 h' }).click();

    await expect(trends).toContainText('Average per hour, last 24 h');
    await expect(page.locator('app-panel').filter({ hasText: 'Rooms compared' })).toContainText(
      '24 h',
    );
  });

  test('has no serious accessibility violations', async ({ page }) => {
    await expect(page.locator('app-room-card').first()).toBeVisible();
    await expectNoSeriousA11yViolations(page);
  });
});
