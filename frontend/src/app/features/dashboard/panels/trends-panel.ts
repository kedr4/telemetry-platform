import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import type { Metric } from '../../../core/graphql/generated';
import { CHARTABLE_METRICS, METRICS } from '../../../core/telemetry/meter-catalog';
import { TelemetryApi } from '../../../core/telemetry/telemetry-api';
import { rangeStart, timeRange, type TimeRangeId } from '../../../core/telemetry/time-range';
import { Chart } from '../../../shared/ui/chart';
import { Panel } from '../../../shared/ui/panel';
import { timeSeriesChart } from '../charts';

/** A metric over time, one line per room. */
@Component({
  selector: 'app-trends-panel',
  imports: [Chart, MatFormFieldModule, MatSelectModule, Panel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-panel heading="Trends" [subheading]="subheading()" [resource]="data">
      <mat-form-field panelActions appearance="outline" subscriptSizing="dynamic">
        <mat-label>Metric</mat-label>
        <mat-select [value]="metric()" (valueChange)="metric.set($event)">
          @for (option of metrics; track option.id) {
            <mat-option [value]="option.id">{{ option.label }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      @if (data.value()?.length) {
        <app-chart
          [options]="chart()"
          [label]="metricLabel() + ' per room over time'"
          height="320px"
        />
      } @else {
        <p class="empty">No readings in this time range yet.</p>
      }
    </app-panel>
  `,
  styles:
    '.empty { text-align: center; padding: 48px 0; color: var(--mat-sys-on-surface-variant); }',
})
export class TrendsPanel {
  readonly range = input.required<TimeRangeId>();
  readonly refreshTick = input(0);

  private readonly api = inject(TelemetryApi);

  protected readonly metrics = CHARTABLE_METRICS;
  protected readonly metric = signal<Metric>('ENERGY');
  protected readonly metricLabel = computed(() => METRICS[this.metric()].label);
  protected readonly subheading = computed(
    () =>
      `Average per ${timeRange(this.range()).bucket.toLowerCase()}, last ${timeRange(this.range()).label}`,
  );

  protected readonly data = rxResource({
    params: () => ({ metric: this.metric(), range: this.range(), tick: this.refreshTick() }),
    stream: ({ params }) =>
      this.api.aggregates({
        groupBy: ['TIME', 'LOCATION'],
        metric: params.metric,
        bucket: timeRange(params.range).bucket,
        filter: { from: rangeStart(params.range) },
      }),
  });

  protected readonly chart = computed(() =>
    timeSeriesChart(this.data.value() ?? [], this.metric(), timeRange(this.range()).bucket),
  );
}
