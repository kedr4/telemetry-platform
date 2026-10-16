import { TIME_RANGES, rangeStart, timeRange } from './time-range';

describe('time ranges', () => {
  it('computes the start relative to now', () => {
    const now = new Date('2026-10-01T12:00:00Z');

    expect(rangeStart('1h', now)).toBe('2026-10-01T11:00:00.000Z');
    expect(rangeStart('7d', now)).toBe('2026-09-24T12:00:00.000Z');
  });

  it('uses coarser buckets for longer ranges', () => {
    expect(timeRange('15m').bucket).toBe('MINUTE');
    expect(timeRange('24h').bucket).toBe('HOUR');
    expect(timeRange('7d').bucket).toBe('DAY');
  });

  it('keeps every range under 400 points per series', () => {
    const bucketMs = { MINUTE: 60_000, HOUR: 3_600_000, DAY: 86_400_000 };
    for (const range of TIME_RANGES) {
      expect(range.durationMs / bucketMs[range.bucket]).toBeLessThanOrEqual(400);
    }
  });
});
