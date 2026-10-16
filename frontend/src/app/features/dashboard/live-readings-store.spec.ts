import { TestBed } from '@angular/core/testing';
import { Subject, of } from 'rxjs';
import type { ReadingsUpdatedNotification } from '../../core/realtime/hub-messages';
import { TelemetryHub } from '../../core/realtime/telemetry-hub';
import { TelemetryApi } from '../../core/telemetry/telemetry-api';
import { LiveReadingsStore, MAX_FEED_ENTRIES } from './live-readings-store';

describe('LiveReadingsStore', () => {
  let updates: Subject<ReadingsUpdatedNotification>;
  let store: LiveReadingsStore;

  const notification = (
    batchId: string,
    time: string,
    co2: number,
  ): ReadingsUpdatedNotification => ({
    batchId,
    recordedAt: `2026-10-01T${time}Z`,
    readings: [
      {
        location: 'Kitchen',
        type: 'AirQuality',
        recordedAt: `2026-10-01T${time}Z`,
        co2,
        pm25: 10,
        humidity: 40,
      },
      {
        location: 'Office',
        type: 'Motion',
        recordedAt: `2026-10-01T${time}Z`,
        motionDetected: true,
      },
    ],
  });

  beforeEach(async () => {
    updates = new Subject();
    TestBed.configureTestingModule({
      providers: [
        LiveReadingsStore,
        { provide: TelemetryHub, useValue: { readingsUpdated$: updates.asObservable() } },
        {
          provide: TelemetryApi,
          useValue: {
            latestReadings: () =>
              of([
                {
                  location: 'Kitchen',
                  type: 'ENERGY',
                  recordedAt: '2026-10-01T11:00:00Z',
                  energy: 5,
                },
              ]),
          },
        },
      ],
    });
    store = TestBed.inject(LiveReadingsStore);
    await vi.waitFor(() => expect(store.latest.hasValue()).toBe(true));
  });

  it('shows the initially loaded readings', () => {
    expect(store.rooms()).toEqual([
      expect.objectContaining({
        location: 'Kitchen',
        energy: expect.objectContaining({ energy: 5 }),
      }),
    ]);
  });

  it('applies live readings on top of the loaded ones', () => {
    updates.next(notification('a', '12:00:00', 600));

    const kitchen = store.rooms().find((room) => room.location === 'Kitchen');
    expect(kitchen?.energy?.energy).toBe(5);
    expect(kitchen?.airQuality?.co2).toBe(600);
    expect(store.rooms().map((room) => room.location)).toEqual(['Kitchen', 'Office']);
  });

  it('records batches in the feed, newest first, with notable events', () => {
    updates.next(notification('a', '12:00:00', 600));
    updates.next(notification('b', '12:00:05', 1500));

    expect(store.feed().map((entry) => entry.batchId)).toEqual(['b', 'a']);
    expect(store.feed()[0].poorAirRooms).toEqual(['Kitchen']);
    expect(store.feed()[0].motionRooms).toEqual(['Office']);
    expect(store.feed()[1].poorAirRooms).toEqual([]);
  });

  it('keeps the feed short', () => {
    for (let i = 0; i < MAX_FEED_ENTRIES + 5; i++) {
      updates.next(notification(`batch-${i}`, '12:00:00', 500));
    }

    expect(store.feed()).toHaveLength(MAX_FEED_ENTRIES);
  });

  it('signals the panels to refresh when data arrives', () => {
    const before = store.refreshTick();

    updates.next(notification('a', '12:00:00', 600));

    expect(store.refreshTick()).toBe(before + 1);
  });
});
