import type { TimeBucket } from '../graphql/generated';

export type TimeRangeId = '15m' | '1h' | '6h' | '24h' | '7d';

export interface TimeRangeOption {
  readonly id: TimeRangeId;
  readonly label: string;
  readonly durationMs: number;
  /** Bucket width that yields a readable number of points for the range. */
  readonly bucket: TimeBucket;
}

const MINUTE = 60_000;
const HOUR = 60 * MINUTE;

export const TIME_RANGES: readonly TimeRangeOption[] = [
  { id: '15m', label: '15 min', durationMs: 15 * MINUTE, bucket: 'MINUTE' },
  { id: '1h', label: '1 h', durationMs: HOUR, bucket: 'MINUTE' },
  { id: '6h', label: '6 h', durationMs: 6 * HOUR, bucket: 'HOUR' },
  { id: '24h', label: '24 h', durationMs: 24 * HOUR, bucket: 'HOUR' },
  { id: '7d', label: '7 days', durationMs: 7 * 24 * HOUR, bucket: 'DAY' },
];

export function timeRange(id: TimeRangeId): TimeRangeOption {
  const range = TIME_RANGES.find((option) => option.id === id);
  if (!range) {
    throw new Error(`Unknown time range '${id}'.`);
  }
  return range;
}

/** Start of the range, as an ISO timestamp, relative to `now`. */
export function rangeStart(id: TimeRangeId, now: Date = new Date()): string {
  return new Date(now.getTime() - timeRange(id).durationMs).toISOString();
}
