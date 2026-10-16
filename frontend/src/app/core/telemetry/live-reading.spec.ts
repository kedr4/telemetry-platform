import type { ReadingFieldsFragment } from '../graphql/generated';
import {
  describeReading,
  fromFragment,
  fromNotification,
  mergeLatest,
  toRoomSummaries,
  type LiveReading,
} from './live-reading';

const at = (time: string) => `2026-10-01T${time}Z`;

const energy = (location: string, value: number, time: string): LiveReading => ({
  location,
  type: 'ENERGY',
  recordedAt: at(time),
  energy: value,
});

describe('live readings', () => {
  it('maps GraphQL fragments by their concrete type', () => {
    const fragment: ReadingFieldsFragment = {
      __typename: 'AirQualityReading',
      id: 1,
      location: 'Garage',
      type: 'AIR_QUALITY',
      recordedAt: at('12:00:00'),
      co2: 543,
      pm25: 26,
      humidity: 68,
    };

    expect(fromFragment(fragment)).toEqual({
      location: 'Garage',
      type: 'AIR_QUALITY',
      recordedAt: at('12:00:00'),
      co2: 543,
      pm25: 26,
      humidity: 68,
    });
  });

  it('maps notification type names to GraphQL meter types', () => {
    const reading = fromNotification({
      location: 'Office',
      type: 'Motion',
      recordedAt: at('12:00:00'),
      motionDetected: true,
    });

    expect(reading.type).toBe('MOTION');
    expect(reading.motionDetected).toBe(true);
  });

  it('keeps the newer reading of each meter', () => {
    const latest = mergeLatest(new Map(), [energy('Kitchen', 1, '12:00:10')]);

    const merged = mergeLatest(latest, [
      energy('Kitchen', 2, '12:00:05'), // older: ignored
      energy('Garage', 3, '12:00:00'),
    ]);

    expect([...merged.values()].map((r) => r.energy)).toEqual([1, 3]);
    expect(
      mergeLatest(merged, [energy('Kitchen', 4, '12:00:20')]).get('Kitchen|ENERGY')?.energy,
    ).toBe(4);
  });

  it('groups latest readings into rooms', () => {
    const latest = mergeLatest(new Map(), [
      energy('Kitchen', 1, '12:00:00'),
      { location: 'Kitchen', type: 'MOTION', recordedAt: at('12:00:30'), motionDetected: false },
      energy('Bedroom', 2, '12:00:00'),
    ]);

    const rooms = toRoomSummaries(latest);

    expect(rooms.map((room) => room.location)).toEqual(['Bedroom', 'Kitchen']);
    expect(rooms[1].energy?.energy).toBe(1);
    expect(rooms[1].motion?.motionDetected).toBe(false);
    expect(rooms[1].updatedAt).toBe(at('12:00:30'));
  });

  it('describes readings for humans', () => {
    expect(describeReading(energy('Kitchen', 604.88, '12:00:00'))).toBe('604.9 kWh');
    expect(
      describeReading({
        location: 'G',
        type: 'AIR_QUALITY',
        recordedAt: '',
        co2: 543,
        pm25: 26,
        humidity: 68,
      }),
    ).toBe('CO₂ 543 ppm · PM2.5 26 µg/m³ · Humidity 68 %');
    expect(
      describeReading({ location: 'O', type: 'MOTION', recordedAt: '', motionDetected: false }),
    ).toBe('No motion');
  });
});
