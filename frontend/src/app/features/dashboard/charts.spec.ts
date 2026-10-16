import type { Aggregate } from '../../core/telemetry/telemetry-api';
import { distributionChart, timeSeriesChart, withGaps } from './charts';

const aggregate = (overrides: Partial<Aggregate>): Aggregate => ({
  location: null,
  type: null,
  bucketStart: null,
  count: 1,
  average: null,
  minimum: null,
  maximum: null,
  sum: null,
  ...overrides,
});

interface LineSeries {
  name: string;
  data: [string, number | null][];
}

describe('dashboard charts', () => {
  it('builds one time-ordered line per location', () => {
    const options = timeSeriesChart(
      [
        aggregate({ location: 'Kitchen', bucketStart: '2026-10-01T12:01:00Z', average: 2 }),
        aggregate({ location: 'Garage', bucketStart: '2026-10-01T12:00:00Z', average: 5 }),
        aggregate({ location: 'Kitchen', bucketStart: '2026-10-01T12:00:00Z', average: 1 }),
      ],
      'ENERGY',
      'MINUTE',
    );

    const series = options['series'] as LineSeries[];
    expect(series.map((s) => s.name)).toEqual(['Garage', 'Kitchen']);
    expect(series[1].data).toEqual([
      ['2026-10-01T12:00:00Z', 1],
      ['2026-10-01T12:01:00Z', 2],
    ]);
  });

  it('breaks lines where no data was collected', () => {
    const points = withGaps(
      [
        ['2026-10-01T12:00:00Z', 1],
        ['2026-10-01T12:01:00Z', 2],
        ['2026-10-01T12:30:00Z', 3],
      ],
      'MINUTE',
    );

    expect(points).toEqual([
      ['2026-10-01T12:00:00Z', 1],
      ['2026-10-01T12:01:00Z', 2],
      ['2026-10-01T12:02:00.000Z', null],
      ['2026-10-01T12:30:00Z', 3],
    ]);
  });

  it('labels the meter type distribution', () => {
    const options = distributionChart([
      aggregate({ type: 'ENERGY', count: 10 }),
      aggregate({ type: 'AIR_QUALITY', count: 5 }),
    ]);

    const [pie] = options['series'] as { data: { name: string; value: number }[] }[];
    expect(pie.data).toEqual([
      { name: 'Energy', value: 10 },
      { name: 'Air quality', value: 5 },
    ]);
  });
});
