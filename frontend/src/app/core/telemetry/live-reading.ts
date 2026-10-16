import type { MeterType, ReadingFieldsFragment } from '../graphql/generated';
import type { ReadingNotification } from '../realtime/hub-messages';
import { formatMetric } from './meter-catalog';

/** A reading in one shape, whether it came from GraphQL or from a live notification. */
export interface LiveReading {
  readonly location: string;
  readonly type: MeterType;
  readonly recordedAt: string;
  readonly energy?: number;
  readonly co2?: number;
  readonly pm25?: number;
  readonly humidity?: number;
  readonly motionDetected?: boolean;
}

export interface RoomSummary {
  readonly location: string;
  readonly energy?: LiveReading;
  readonly airQuality?: LiveReading;
  readonly motion?: LiveReading;
  /** Most recent reading time across the room's meters. */
  readonly updatedAt: string;
}

const NOTIFICATION_TYPES: Readonly<Record<ReadingNotification['type'], MeterType>> = {
  Energy: 'ENERGY',
  AirQuality: 'AIR_QUALITY',
  Motion: 'MOTION',
};

export function fromFragment(reading: ReadingFieldsFragment): LiveReading {
  const base = { location: reading.location, type: reading.type, recordedAt: reading.recordedAt };
  switch (reading.__typename) {
    case 'EnergyReading':
      return { ...base, energy: reading.energy };
    case 'AirQualityReading':
      return { ...base, co2: reading.co2, pm25: reading.pm25, humidity: reading.humidity };
    case 'MotionReading':
      return { ...base, motionDetected: reading.motionDetected };
  }
}

export function fromNotification(reading: ReadingNotification): LiveReading {
  return { ...reading, type: NOTIFICATION_TYPES[reading.type] };
}

export function readingKey(reading: Pick<LiveReading, 'location' | 'type'>): string {
  return `${reading.location}|${reading.type}`;
}

/** Adds readings to a latest-per-meter map, keeping whichever reading of a meter is newer. */
export function mergeLatest(
  latest: ReadonlyMap<string, LiveReading>,
  readings: readonly LiveReading[],
): Map<string, LiveReading> {
  const merged = new Map(latest);
  for (const reading of readings) {
    const key = readingKey(reading);
    const current = merged.get(key);
    if (!current || Date.parse(reading.recordedAt) >= Date.parse(current.recordedAt)) {
      merged.set(key, reading);
    }
  }
  return merged;
}

/** Groups the latest readings by room, alphabetically. */
export function toRoomSummaries(latest: ReadonlyMap<string, LiveReading>): RoomSummary[] {
  const rooms = new Map<string, { -readonly [K in keyof RoomSummary]: RoomSummary[K] }>();
  for (const reading of latest.values()) {
    const room = rooms.get(reading.location) ?? {
      location: reading.location,
      updatedAt: reading.recordedAt,
    };
    if (Date.parse(reading.recordedAt) > Date.parse(room.updatedAt)) {
      room.updatedAt = reading.recordedAt;
    }
    if (reading.type === 'ENERGY') {
      room.energy = reading;
    } else if (reading.type === 'AIR_QUALITY') {
      room.airQuality = reading;
    } else {
      room.motion = reading;
    }
    rooms.set(reading.location, room);
  }
  return [...rooms.values()].sort((a, b) => a.location.localeCompare(b.location));
}

/** A one-line, human-readable description of a reading's values. */
export function describeReading(reading: LiveReading): string {
  switch (reading.type) {
    case 'ENERGY':
      return formatMetric('ENERGY', reading.energy);
    case 'AIR_QUALITY':
      return [
        `CO₂ ${formatMetric('CO2', reading.co2)}`,
        `PM2.5 ${formatMetric('PM25', reading.pm25)}`,
        `Humidity ${formatMetric('HUMIDITY', reading.humidity)}`,
      ].join(' · ');
    case 'MOTION':
      return reading.motionDetected ? 'Motion detected' : 'No motion';
  }
}
