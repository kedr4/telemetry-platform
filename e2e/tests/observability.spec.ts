import { expect, test, type APIRequestContext } from '@playwright/test';
import { urls } from '../playwright.config';

interface PrometheusTargets {
  data: { activeTargets: { labels: Record<string, string>; health: string }[] };
}

interface PrometheusVector {
  data: { result: { value: [number, string] }[] };
}

interface PrometheusRules {
  data: { groups: { rules: { name: string; state: string }[] }[] };
}

async function scalar(request: APIRequestContext, query: string): Promise<number> {
  const response = await request.get(`${urls.prometheus}/api/v1/query`, { params: { query } });
  const { data } = (await response.json()) as PrometheusVector;
  return Number(data.result[0]?.value[1] ?? 0);
}

test.describe('Monitoring', () => {
  test('Prometheus scrapes every service and dependency', async ({ request }) => {
    await expect
      .poll(
        async () => {
          const response = await request.get(`${urls.prometheus}/api/v1/targets`);
          const { data } = (await response.json()) as PrometheusTargets;
          return Object.fromEntries(
            data.activeTargets.map((target) => [
              target.labels['service'] ?? target.labels['job'],
              target.health,
            ]),
          );
        },
        { timeout: 45_000 },
      )
      .toMatchObject({
        ingestor: 'up',
        processor: 'up',
        gateway: 'up',
        notifications: 'up',
        rabbitmq: 'up',
        postgres: 'up',
      });
  });

  test('pipeline metrics show readings flowing end to end', async ({ request }) => {
    await expect
      .poll(() => scalar(request, 'sum(telemetry_processor_readings_stored_total)'), {
        timeout: 45_000,
      })
      .toBeGreaterThan(0);
    expect(
      await scalar(request, 'sum(telemetry_ingestor_readings_published_total)'),
    ).toBeGreaterThan(0);
    expect(
      await scalar(request, 'sum(telemetry_messaging_consumed_total{outcome="acked"})'),
    ).toBeGreaterThan(0);
  });

  test('alert rules are loaded and no service is down', async ({ request }) => {
    const { data } = (await (
      await request.get(`${urls.prometheus}/api/v1/rules`)
    ).json()) as PrometheusRules;
    const states = Object.fromEntries(
      data.groups.flatMap((group) => group.rules).map((rule) => [rule.name, rule.state]),
    );

    expect(Object.keys(states)).toEqual(
      expect.arrayContaining(['ServiceDown', 'IngestionStalled', 'MessagesDeadLettered']),
    );
    expect(states['ServiceDown']).not.toBe('firing');
  });

  test('Grafana serves the provisioned dashboards', async ({ request }) => {
    for (const uid of ['telemetry-overview', 'telemetry-runtime']) {
      const response = await request.get(`${urls.grafana}/api/dashboards/uid/${uid}`);
      expect(response.ok(), `dashboard ${uid}`).toBeTruthy();
      const { dashboard } = (await response.json()) as { dashboard: { panels: unknown[] } };
      expect(dashboard.panels.length).toBeGreaterThan(10);
    }
  });
});
