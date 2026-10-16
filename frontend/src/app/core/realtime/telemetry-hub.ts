import { Injectable, InjectionToken, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type IRetryPolicy,
} from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { HUB_PATH, READINGS_UPDATED, type ReadingsUpdatedNotification } from './hub-messages';

export type HubStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

/** Exponential back-off capped at 30 s, retrying forever: the dashboard should always come back. */
export const RECONNECT_POLICY: IRetryPolicy = {
  nextRetryDelayInMilliseconds: ({ previousRetryCount }) =>
    Math.min(30_000, 1_000 * 2 ** Math.min(previousRetryCount, 5)),
};

/** Creates the hub connection; replaced by a fake in tests. */
export const HUB_CONNECTION_FACTORY = new InjectionToken<() => HubConnection>(
  'HUB_CONNECTION_FACTORY',
  {
    providedIn: 'root',
    factory: () => () =>
      new HubConnectionBuilder()
        .withUrl(HUB_PATH)
        .withAutomaticReconnect(RECONNECT_POLICY)
        .configureLogging(LogLevel.Warning)
        .build(),
  },
);

const REMEMBERED_BATCHES = 100;

/**
 * Live connection to the notifications service. Exposes the connection status as a signal and new
 * readings as a stream. Notifications are at-least-once, so repeated batches are filtered out.
 */
@Injectable({ providedIn: 'root' })
export class TelemetryHub {
  private readonly connection = inject(HUB_CONNECTION_FACTORY)();
  private readonly statusSignal = signal<HubStatus>('disconnected');
  private readonly lastUpdateSignal = signal<Date | null>(null);
  private readonly updates = new Subject<ReadingsUpdatedNotification>();
  private readonly seenBatches: string[] = [];
  private started = false;
  private startAttempt = 0;

  readonly status = this.statusSignal.asReadonly();
  readonly lastUpdate = this.lastUpdateSignal.asReadonly();
  readonly readingsUpdated$: Observable<ReadingsUpdatedNotification> = this.updates.asObservable();

  constructor() {
    this.connection.on(READINGS_UPDATED, (notification: ReadingsUpdatedNotification) =>
      this.receive(notification),
    );
    this.connection.onreconnecting(() => this.statusSignal.set('reconnecting'));
    this.connection.onreconnected(() => this.statusSignal.set('connected'));
    this.connection.onclose(() => {
      this.statusSignal.set('disconnected');
      // Automatic reconnect gave up (or the server closed the connection): keep trying.
      if (this.started) {
        this.scheduleStart();
      }
    });
  }

  /** Connects, retrying with back-off until it succeeds. Safe to call more than once. */
  start(): void {
    if (this.started) {
      return;
    }
    this.started = true;
    void this.connect();
  }

  async stop(): Promise<void> {
    this.started = false;
    await this.connection.stop();
  }

  private async connect(): Promise<void> {
    if (!this.started || this.connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.statusSignal.set('connecting');
    try {
      await this.connection.start();
      this.startAttempt = 0;
      this.statusSignal.set('connected');
    } catch {
      this.statusSignal.set('disconnected');
      this.scheduleStart();
    }
  }

  private scheduleStart(): void {
    const delay = RECONNECT_POLICY.nextRetryDelayInMilliseconds({
      previousRetryCount: this.startAttempt++,
      elapsedMilliseconds: 0,
      retryReason: new Error('Connection lost'),
    });
    setTimeout(() => void this.connect(), delay ?? 30_000);
  }

  private receive(notification: ReadingsUpdatedNotification): void {
    if (this.seenBatches.includes(notification.batchId)) {
      return;
    }

    this.seenBatches.push(notification.batchId);
    if (this.seenBatches.length > REMEMBERED_BATCHES) {
      this.seenBatches.shift();
    }

    this.lastUpdateSignal.set(new Date());
    this.updates.next(notification);
  }
}
