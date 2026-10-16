import { TestBed } from '@angular/core/testing';
import { HubConnectionState, type HubConnection } from '@microsoft/signalr';
import type { ReadingsUpdatedNotification } from './hub-messages';
import { HUB_CONNECTION_FACTORY, RECONNECT_POLICY, TelemetryHub } from './telemetry-hub';

/** Minimal stand-in for a SignalR connection that lets tests raise its events. */
class FakeConnection {
  state = HubConnectionState.Disconnected;
  readonly handlers = new Map<string, (payload: unknown) => void>();
  reconnecting?: () => void;
  reconnected?: () => void;
  closed?: () => void;
  start = vi.fn(async () => {
    this.state = HubConnectionState.Connected;
  });
  stop = vi.fn(async () => {
    this.state = HubConnectionState.Disconnected;
  });

  on(name: string, handler: (payload: unknown) => void) {
    this.handlers.set(name, handler);
  }
  onreconnecting(callback: () => void) {
    this.reconnecting = callback;
  }
  onreconnected(callback: () => void) {
    this.reconnected = callback;
  }
  onclose(callback: () => void) {
    this.closed = callback;
  }
  push(notification: ReadingsUpdatedNotification) {
    this.handlers.get('ReadingsUpdated')?.(notification);
  }
}

const notification = (batchId: string): ReadingsUpdatedNotification => ({
  batchId,
  recordedAt: '2026-10-01T12:00:00Z',
  readings: [
    { location: 'Kitchen', type: 'Energy', recordedAt: '2026-10-01T12:00:00Z', energy: 1 },
  ],
});

describe('TelemetryHub', () => {
  let connection: FakeConnection;
  let hub: TelemetryHub;

  beforeEach(() => {
    connection = new FakeConnection();
    TestBed.configureTestingModule({
      providers: [
        {
          provide: HUB_CONNECTION_FACTORY,
          useValue: () => connection as unknown as HubConnection,
        },
      ],
    });
    hub = TestBed.inject(TelemetryHub);
  });

  it('reports the connection status', async () => {
    expect(hub.status()).toBe('disconnected');

    hub.start();
    expect(hub.status()).toBe('connecting');
    await vi.waitFor(() => expect(hub.status()).toBe('connected'));

    connection.reconnecting?.();
    expect(hub.status()).toBe('reconnecting');
    connection.reconnected?.();
    expect(hub.status()).toBe('connected');
  });

  it('starts only once', async () => {
    hub.start();
    hub.start();

    await vi.waitFor(() => expect(hub.status()).toBe('connected'));
    expect(connection.start).toHaveBeenCalledTimes(1);
  });

  it('emits notifications and ignores repeated batches', () => {
    const received: string[] = [];
    hub.readingsUpdated$.subscribe((n) => received.push(n.batchId));

    connection.push(notification('a'));
    connection.push(notification('b'));
    connection.push(notification('a'));

    expect(received).toEqual(['a', 'b']);
    expect(hub.lastUpdate()).toBeInstanceOf(Date);
  });

  it('retries a failed start with back-off', async () => {
    vi.useFakeTimers();
    try {
      connection.start
        .mockImplementationOnce(async () => {
          throw new Error('Server unavailable');
        })
        .mockImplementationOnce(async () => {
          connection.state = HubConnectionState.Connected;
        });

      hub.start();
      await vi.advanceTimersByTimeAsync(0);
      expect(hub.status()).toBe('disconnected');

      await vi.advanceTimersByTimeAsync(1_000);
      expect(connection.start).toHaveBeenCalledTimes(2);
      expect(hub.status()).toBe('connected');
    } finally {
      vi.useRealTimers();
    }
  });

  it('backs off exponentially up to 30 seconds', () => {
    const delay = (previousRetryCount: number) =>
      RECONNECT_POLICY.nextRetryDelayInMilliseconds({
        previousRetryCount,
        elapsedMilliseconds: 0,
        retryReason: new Error(),
      });

    expect([0, 1, 2, 3, 4, 5, 10].map(delay)).toEqual([
      1_000, 2_000, 4_000, 8_000, 16_000, 30_000, 30_000,
    ]);
  });
});
