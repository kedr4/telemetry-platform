/** Payloads pushed by the notifications service (SignalR hub /hubs/telemetry). */

export interface ReadingNotification {
  readonly location: string;
  readonly type: 'Energy' | 'AirQuality' | 'Motion';
  readonly recordedAt: string;
  readonly energy?: number;
  readonly co2?: number;
  readonly pm25?: number;
  readonly humidity?: number;
  readonly motionDetected?: boolean;
}

export interface ReadingsUpdatedNotification {
  readonly batchId: string;
  readonly recordedAt: string;
  readonly readings: readonly ReadingNotification[];
}

export const HUB_PATH = '/hubs/telemetry';
export const READINGS_UPDATED = 'ReadingsUpdated';
