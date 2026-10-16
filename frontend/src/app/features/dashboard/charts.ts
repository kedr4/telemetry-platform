import type { MeterType, Metric, TimeBucket } from '../../core/graphql/generated';
import { METER_TYPES, METRICS, formatMetric } from '../../core/telemetry/meter-catalog';
import type { Aggregate } from '../../core/telemetry/telemetry-api';
import type { EChartsCoreOption } from '../../shared/ui/echarts';

const BUCKET_MS: Readonly<Record<TimeBucket, number>> = {
  MINUTE: 60_000,
  HOUR: 3_600_000,
  DAY: 86_400_000,
};

type Point = [string, number | null];

/**
 * Inserts a null point where consecutive buckets are further apart than one bucket, so the chart shows
 * a gap (no data was collected) instead of drawing a line across it.
 */
export function withGaps(points: readonly [string, number][], bucket: TimeBucket): Point[] {
  const sorted = [...points].sort(([a], [b]) => Date.parse(a) - Date.parse(b));
  const result: Point[] = [];
  for (const [index, point] of sorted.entries()) {
    const previous = sorted[index - 1];
    if (previous && Date.parse(point[0]) - Date.parse(previous[0]) > BUCKET_MS[bucket] * 1.5) {
      result.push([new Date(Date.parse(previous[0]) + BUCKET_MS[bucket]).toISOString(), null]);
    }
    result.push(point);
  }
  return result;
}

/** One line per location over time (aggregates grouped by TIME and LOCATION). */
export function timeSeriesChart(
  aggregates: readonly Aggregate[],
  metric: Metric,
  bucket: TimeBucket,
): EChartsCoreOption {
  const byLocation = new Map<string, [string, number][]>();
  for (const aggregate of aggregates) {
    if (
      aggregate.location === null ||
      aggregate.bucketStart === null ||
      aggregate.average === null
    ) {
      continue;
    }
    const points = byLocation.get(aggregate.location) ?? [];
    points.push([aggregate.bucketStart, aggregate.average]);
    byLocation.set(aggregate.location, points);
  }

  const series = [...byLocation.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([location, points]) => ({
      name: location,
      type: 'line',
      showSymbol: points.length < 3,
      connectNulls: false,
      data: withGaps(points, bucket),
    }));

  return {
    tooltip: { trigger: 'axis', valueFormatter: (value: number) => formatMetric(metric, value) },
    legend: { type: 'scroll', bottom: 0 },
    grid: { left: 8, right: 16, top: 32, bottom: 40, containLabel: true },
    xAxis: { type: 'time', axisLabel: { hideOverlap: true } },
    yAxis: { type: 'value', name: METRICS[metric].unit, scale: true },
    series,
  };
}

/** Average and maximum of a metric per location (aggregates grouped by LOCATION). */
export function comparisonChart(
  aggregates: readonly Aggregate[],
  metric: Metric,
): EChartsCoreOption {
  const rows = aggregates
    .filter((aggregate) => aggregate.location !== null)
    .sort((a, b) => (a.location ?? '').localeCompare(b.location ?? ''));

  return {
    tooltip: { trigger: 'axis', valueFormatter: (value: number) => formatMetric(metric, value) },
    legend: { bottom: 0 },
    grid: { left: 8, right: 16, top: 32, bottom: 40, containLabel: true },
    xAxis: {
      type: 'category',
      data: rows.map((row) => row.location),
      axisLabel: { interval: 0, rotate: 30 },
    },
    yAxis: { type: 'value', name: METRICS[metric].unit },
    series: [
      { name: 'Average', type: 'bar', data: rows.map((row) => round(row.average)) },
      { name: 'Maximum', type: 'bar', data: rows.map((row) => round(row.maximum)) },
    ],
  };
}

/** Share of readings per meter type (aggregates grouped by TYPE, without a metric). */
export function distributionChart(aggregates: readonly Aggregate[]): EChartsCoreOption {
  const data = aggregates
    .filter((aggregate): aggregate is Aggregate & { type: MeterType } => aggregate.type !== null)
    .map((aggregate) => ({ name: METER_TYPES[aggregate.type].label, value: aggregate.count }));

  return {
    tooltip: { trigger: 'item', formatter: '{b}: {c} readings ({d}%)' },
    legend: { bottom: 0 },
    series: [
      {
        name: 'Readings',
        type: 'pie',
        radius: ['45%', '70%'],
        center: ['50%', '45%'],
        label: { position: 'inside', formatter: '{d}%' },
        data,
      },
    ],
  };
}

function round(value: number | null): number | null {
  return value === null ? null : Math.round(value * 10) / 10;
}
