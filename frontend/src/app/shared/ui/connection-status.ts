import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TelemetryHub, type HubStatus } from '../../core/realtime/telemetry-hub';

const LABELS: Readonly<Record<HubStatus, string>> = {
  connected: 'Live',
  connecting: 'Connecting…',
  reconnecting: 'Reconnecting…',
  disconnected: 'Offline',
};

/** Shows whether live updates are flowing, and when the last one arrived. */
@Component({
  selector: 'app-connection-status',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="'status status-' + status()" role="status" [attr.title]="tooltip()">
      <span class="dot" aria-hidden="true"></span>
      <span class="label">{{ label() }}</span>
      @if (lastUpdate(); as last) {
        <span class="last-update">· {{ last | date: 'HH:mm:ss' }}</span>
      }
    </span>
  `,
  styles: `
    .status {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 4px 12px;
      border-radius: 16px;
      font: var(--mat-sys-label-large);
      background: var(--mat-sys-surface-container-high);
      color: var(--mat-sys-on-surface);
    }
    .dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: var(--mat-sys-outline);
    }
    .status-connected .dot {
      background: #2e7d32;
      box-shadow: 0 0 0 3px rgb(46 125 50 / 25%);
    }
    .status-connecting .dot,
    .status-reconnecting .dot {
      background: #f9a825;
    }
    .status-disconnected .dot {
      background: var(--mat-sys-error);
    }
    @media (max-width: 599px) {
      .last-update {
        display: none;
      }
    }
  `,
})
export class ConnectionStatus {
  private readonly hub = inject(TelemetryHub);

  protected readonly status = this.hub.status;
  protected readonly lastUpdate = this.hub.lastUpdate;
  protected readonly label = computed(() => LABELS[this.status()]);
  protected readonly tooltip = computed(() =>
    this.status() === 'connected'
      ? 'Receiving live readings'
      : 'Live updates paused; data shown may be out of date',
  );
}
