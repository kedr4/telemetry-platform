import { defineConfig, devices } from '@playwright/test';

/**
 * Runs against an already running platform (`docker compose up --build --wait`). Every address can be
 * overridden, e.g. to test a deployed environment.
 */
export const urls = {
  app: process.env['E2E_APP_URL'] ?? 'http://localhost:4200',
  gateway: process.env['E2E_GATEWAY_URL'] ?? 'http://localhost:5103',
  prometheus: process.env['E2E_PROMETHEUS_URL'] ?? 'http://localhost:9090',
  grafana: process.env['E2E_GRAFANA_URL'] ?? 'http://localhost:3000',
};

const ci = !!process.env['CI'];

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: ci,
  // WeakApp is unstable by design; a retry absorbs an unlucky stretch without hiding real failures.
  retries: ci ? 1 : 0,
  workers: ci ? 2 : undefined,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: ci
    ? [['github'], ['html', { open: 'never' }], ['list']]
    : [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: urls.app,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    // Backend and monitoring checks do not need a browser per device.
    { name: 'api', testMatch: /(api|observability)\.spec\.ts/ },
    {
      name: 'desktop',
      testIgnore: /(api|observability)\.spec\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
    { name: 'mobile', testIgnore: /(api|observability)\.spec\.ts/, use: { ...devices['Pixel 7'] } },
  ],
});
