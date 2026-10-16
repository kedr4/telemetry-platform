import { Injectable, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { asyncScheduler, throttleTime } from 'rxjs';
import type { ReadingsUpdatedNotification } from '../../core/realtime/hub-messages';
import { TelemetryHub } from '../../core/realtime/telemetry-hub';
import {
  fromNotification,
  mergeLatest,
  toRoomSummaries,
  type LiveReading,
} from '../../core/telemetry/live-reading';
import { co2Level } from '../../core/telemetry/meter-catalog';
import { TelemetryApi } from '../../core/telemetry/telemetry-api';

export interface FeedEntry {
  readonly batchId: string;
  readonly recordedAt: string;
  readonly readingCount: number;
  readonly motionRooms: readonly string[];
  readonly poorAirRooms: readonly string[];
}

export const MAX_FEED_ENTRIES = 15;

/** Aggregation panels re-query at most this often while live data keeps arriving. */
export const REFRESH_THROTTLE_MS = 15_000;

export function toFeedEntry(
  notification: ReadingsUpdatedNotification,
  readings: readonly LiveReading[],
): FeedEntry {
  return {
    batchId: notification.batchId,
    recordedAt: notification.recordedAt,
    readingCount: readings.length,
    motionRooms: readings.filter((r) => r.motionDetected).map((r) => r.location),
    poorAirRooms: readings
      .filter((r) => r.co2 !== undefined && co2Level(r.co2) === 'poor')
      .map((r) => r.location),
  };
}

/**
 * Dashboard state: the latest reading of every meter (loaded once, then kept current by live
 * notifications without re-querying), a feed of recent batches, and a throttled refresh tick that tells
 * the aggregation panels when re-querying is worthwhile.
 */
@Injectable()
export class LiveReadingsStore {
  private readonly api = inject(TelemetryApi);
  private readonly hub = inject(TelemetryHub);
  private readonly live = signal<ReadonlyMap<string, LiveReading>>(new Map());
  private readonly feedSignal = signal<readonly FeedEntry[]>([]);
  private readonly refreshTickSignal = signal(0);

  /** Initial latest readings from the gateway. */
  readonly latest = rxResource({ stream: () => this.api.latestReadings() });

  readonly rooms = computed(() =>
    toRoomSummaries(
      mergeLatest(new Map(), [...(this.latest.value() ?? []), ...this.live().values()]),
    ),
  );
  readonly feed = this.feedSignal.asReadonly();
  readonly refreshTick = this.refreshTickSignal.asReadonly();

  constructor() {
    this.hub.readingsUpdated$
      .pipe(takeUntilDestroyed())
      .subscribe((notification) => this.apply(notification));

    this.hub.readingsUpdated$
      .pipe(
        throttleTime(REFRESH_THROTTLE_MS, asyncScheduler, { leading: true, trailing: true }),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.refreshTickSignal.update((tick) => tick + 1));
  }

  private apply(notification: ReadingsUpdatedNotification): void {
    const readings = notification.readings.map(fromNotification);
    this.live.update((latest) => mergeLatest(latest, readings));
    this.feedSignal.update((feed) =>
      [toFeedEntry(notification, readings), ...feed].slice(0, MAX_FEED_ENTRIES),
    );
  }
}
