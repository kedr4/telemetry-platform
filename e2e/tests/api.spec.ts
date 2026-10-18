import { expect, test } from '@playwright/test';
import { urls } from '../playwright.config';
import { graphql, locations } from '../support/gateway';

interface RestReading {
  id: number;
  type: 'Energy' | 'AirQuality' | 'Motion';
  location: string;
  recordedAt: string;
}

interface RestPage {
  items: RestReading[];
  totalCount: number;
  hasNextPage: boolean;
  endCursor: string | null;
}

const TYPE_SPECIFIC_FIELD = {
  Energy: 'energy',
  AirQuality: 'co2',
  Motion: 'motionDetected',
} as const;

test.describe('REST API', () => {
  test('lists the rooms that have meters, alphabetically', async ({ request }) => {
    const rooms = await locations(request);

    expect(rooms).toEqual([...new Set(rooms)].sort((a, b) => a.localeCompare(b)));
  });

  test('serializes readings polymorphically by type', async ({ request }) => {
    const response = await request.get(`${urls.gateway}/api/v1/readings/latest`);
    expect(response.ok()).toBeTruthy();
    const readings = (await response.json()) as RestReading[];

    expect(readings.length).toBeGreaterThan(0);
    for (const reading of readings) {
      expect(reading).toHaveProperty(TYPE_SPECIFIC_FIELD[reading.type]);
    }
  });

  test('pages with a cursor, without overlap', async ({ request }) => {
    const getPage = async (after?: string) => {
      const params: Record<string, string | number> = { first: 10 };
      if (after) {
        params['after'] = after;
      }
      const response = await request.get(`${urls.gateway}/api/v1/readings`, { params });
      expect(response.ok()).toBeTruthy();
      return (await response.json()) as RestPage;
    };

    const first = await getPage();
    expect(first.items).toHaveLength(10);
    expect(first.hasNextPage).toBe(true);
    const second = await getPage(first.endCursor ?? undefined);

    const firstIds = new Set(first.items.map((reading) => reading.id));
    expect(second.items.filter((reading) => firstIds.has(reading.id))).toEqual([]);
    expect(Date.parse(second.items[0]!.recordedAt)).toBeLessThanOrEqual(
      Date.parse(first.items.at(-1)!.recordedAt),
    );
  });

  test('filters by room and meter type', async ({ request }) => {
    const [room] = await locations(request);
    const response = await request.get(`${urls.gateway}/api/v1/readings`, {
      params: { location: room!, type: 'Energy', first: 20 },
    });
    const page = (await response.json()) as RestPage;

    expect(page.items.length).toBeGreaterThan(0);
    for (const reading of page.items) {
      expect(reading).toMatchObject({ location: room, type: 'Energy' });
    }
  });

  test('rejects invalid input with problem details', async ({ request }) => {
    const response = await request.get(`${urls.gateway}/api/v1/readings`, { params: { first: 0 } });

    expect(response.status()).toBe(400);
    expect(response.headers()['content-type']).toContain('application/problem+json');
    const problem = (await response.json()) as { status: number; errors: Record<string, string[]> };
    expect(problem.status).toBe(400);
    expect(problem.errors).toHaveProperty('First');
  });
});

test.describe('GraphQL API', () => {
  test('returns typed readings through fragments', async ({ request }) => {
    const result = await graphql<{ latestReadings: { __typename: string }[] }>(
      request,
      `
        {
          latestReadings {
            __typename
            location
            ... on EnergyReading {
              energy
            }
            ... on AirQualityReading {
              co2
              pm25
              humidity
            }
            ... on MotionReading {
              motionDetected
            }
          }
        }
      `,
    );

    expect(result.errors).toBeUndefined();
    const typenames = new Set(result.data!.latestReadings.map((reading) => reading.__typename));
    expect([...typenames].sort()).toEqual(['AirQualityReading', 'EnergyReading', 'MotionReading']);
  });

  test('aggregates a metric per room', async ({ request }) => {
    const rooms = await locations(request);
    const result = await graphql<{
      aggregates: {
        location: string;
        count: number;
        average: number;
        minimum: number;
        maximum: number;
      }[];
    }>(
      request,
      `
        query ($input: AggregationInput!) {
          aggregates(input: $input) {
            location
            count
            average
            minimum
            maximum
          }
        }
      `,
      { input: { groupBy: ['LOCATION'], metric: 'ENERGY' } },
    );

    expect(result.errors).toBeUndefined();
    const aggregates = result.data!.aggregates;
    expect(aggregates.map((aggregate) => aggregate.location).sort()).toEqual([...rooms].sort());
    for (const aggregate of aggregates) {
      expect(aggregate.count).toBeGreaterThan(0);
      expect(aggregate.minimum).toBeLessThanOrEqual(aggregate.average);
      expect(aggregate.average).toBeLessThanOrEqual(aggregate.maximum);
    }
  });

  test('reports validation errors per field', async ({ request }) => {
    const result = await graphql(request, '{ readings(first: 0) { totalCount } }');

    expect(result.data).toBeNull();
    expect(result.errors?.[0]?.extensions).toMatchObject({
      code: 'VALIDATION_ERROR',
      errors: [{ field: 'First' }],
    });
  });
});
