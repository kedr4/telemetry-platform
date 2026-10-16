import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import type { RoomSummary } from '../../core/telemetry/live-reading';
import { co2Level, formatMetric, pm25Level } from '../../core/telemetry/meter-catalog';

/** Latest values of one room: energy, air quality (with comfort levels) and motion. */
@Component({
  selector: 'app-room-card',
  imports: [DatePipe, MatCardModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @let r = room();
    <mat-card appearance="outlined">
      <mat-card-header>
        <mat-card-title>{{ r.location }}</mat-card-title>
        <mat-card-subtitle>{{ r.updatedAt | date: 'HH:mm:ss' }}</mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <dl>
          <div class="metric">
            <dt><mat-icon aria-hidden="true">bolt</mat-icon> Energy</dt>
            <dd>{{ energy() }}</dd>
          </div>
          <div class="metric">
            <dt><mat-icon aria-hidden="true">co2</mat-icon> CO₂</dt>
            <dd>
              {{ co2() }}
              @if (co2Status(); as level) {
                <span [class]="'level level-' + level" [attr.title]="level + ' air quality'"></span>
                <span class="visually-hidden">({{ level }})</span>
              }
            </dd>
          </div>
          <div class="metric">
            <dt><mat-icon aria-hidden="true">grain</mat-icon> PM2.5</dt>
            <dd>
              {{ pm25() }}
              @if (pm25Status(); as level) {
                <span [class]="'level level-' + level" [attr.title]="level + ' air quality'"></span>
                <span class="visually-hidden">({{ level }})</span>
              }
            </dd>
          </div>
          <div class="metric">
            <dt><mat-icon aria-hidden="true">water_drop</mat-icon> Humidity</dt>
            <dd>{{ humidity() }}</dd>
          </div>
          <div class="metric">
            <dt><mat-icon aria-hidden="true">directions_walk</mat-icon> Motion</dt>
            <dd [class.motion-active]="r.motion?.motionDetected">{{ motion() }}</dd>
          </div>
        </dl>
      </mat-card-content>
    </mat-card>
  `,
  styleUrl: './room-card.scss',
})
export class RoomCard {
  readonly room = input.required<RoomSummary>();

  protected readonly energy = computed(() => formatMetric('ENERGY', this.room().energy?.energy));
  protected readonly co2 = computed(() => formatMetric('CO2', this.room().airQuality?.co2));
  protected readonly pm25 = computed(() => formatMetric('PM25', this.room().airQuality?.pm25));
  protected readonly humidity = computed(() =>
    formatMetric('HUMIDITY', this.room().airQuality?.humidity),
  );

  protected readonly co2Status = computed(() => {
    const co2 = this.room().airQuality?.co2;
    return co2 === undefined ? null : co2Level(co2);
  });

  protected readonly pm25Status = computed(() => {
    const pm25 = this.room().airQuality?.pm25;
    return pm25 === undefined ? null : pm25Level(pm25);
  });

  protected readonly motion = computed(() => {
    const detected = this.room().motion?.motionDetected;
    if (detected === undefined) {
      return '–';
    }
    return detected ? 'Detected' : 'None';
  });
}
