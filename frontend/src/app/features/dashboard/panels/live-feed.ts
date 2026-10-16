import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import type { FeedEntry } from '../live-readings-store';

/** The most recent batches pushed over SignalR, with notable events highlighted. */
@Component({
  selector: 'app-live-feed',
  imports: [DatePipe, MatCardModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-card appearance="outlined">
      <mat-card-header>
        <mat-card-title>Live feed</mat-card-title>
        <mat-card-subtitle>Batches as they are stored</mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <ol class="feed" aria-live="polite">
          @for (entry of entries(); track entry.batchId) {
            <li>
              <time [attr.datetime]="entry.recordedAt">{{
                entry.recordedAt | date: 'HH:mm:ss'
              }}</time>
              <span>{{ entry.readingCount }} readings</span>
              @if (entry.motionRooms.length) {
                <span class="event" title="Motion detected">
                  <mat-icon aria-hidden="true">directions_walk</mat-icon
                  >{{ entry.motionRooms.join(', ') }}
                </span>
              }
              @if (entry.poorAirRooms.length) {
                <span class="event warn" title="Poor air quality (CO₂)">
                  <mat-icon aria-hidden="true">warning</mat-icon>{{ entry.poorAirRooms.join(', ') }}
                </span>
              }
            </li>
          } @empty {
            <li class="waiting">Waiting for live readings…</li>
          }
        </ol>
      </mat-card-content>
    </mat-card>
  `,
  styles: `
    :host {
      display: block;
    }
    mat-card {
      height: 100%;
    }
    .feed {
      list-style: none;
      margin: 0;
      padding: 0;
      max-height: 340px;
      overflow-y: auto;
    }
    li {
      display: flex;
      flex-wrap: wrap;
      gap: 4px 12px;
      align-items: center;
      padding: 8px 0;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    time {
      font-variant-numeric: tabular-nums;
      font-weight: 500;
    }
    .event {
      display: inline-flex;
      align-items: center;
      gap: 2px;
      color: var(--mat-sys-primary);
    }
    .event mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }
    .warn {
      color: var(--mat-sys-error);
    }
    .waiting {
      color: var(--mat-sys-on-surface-variant);
      border: none;
    }
  `,
})
export class LiveFeed {
  readonly entries = input.required<readonly FeedEntry[]>();
}
