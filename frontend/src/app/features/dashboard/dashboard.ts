import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { TIME_RANGES, type TimeRangeId } from '../../core/telemetry/time-range';
import { Panel } from '../../shared/ui/panel';
import { LiveReadingsStore } from './live-readings-store';
import { LiveFeed } from './panels/live-feed';
import { MeterTypesPanel } from './panels/meter-types-panel';
import { RoomComparisonPanel } from './panels/room-comparison-panel';
import { TrendsPanel } from './panels/trends-panel';
import { RoomCard } from './room-card';

/** Overview: live values per room, trends, comparisons and the live feed. */
@Component({
  selector: 'app-dashboard',
  imports: [
    LiveFeed,
    MatButtonToggleModule,
    MeterTypesPanel,
    Panel,
    RoomCard,
    RoomComparisonPanel,
    TrendsPanel,
  ],
  providers: [LiveReadingsStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  protected readonly store = inject(LiveReadingsStore);
  protected readonly ranges = TIME_RANGES;
  protected readonly range = signal<TimeRangeId>('1h');
}
